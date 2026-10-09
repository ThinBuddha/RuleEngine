using Rules.Expression;
using Rules.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rules.Rules
{
    public enum EvaluationType
    {
        FireOnce,    // Each rule can be visited maximal one time 
        Iteration,   // Retry rules until no new rule fires; each rule fires at most once per evaluation.
        Deduction    // Each rule can be visited many times, starting with the conclusion ("backward chaining")
    };

    /// <summary>
    /// For each evaluation, instantiate a <see cref="RuleEngine"/> with its own rule graph and context.
    /// </summary>
    public class RuleEngine
    {
        private readonly RuleContainer _ruleContainer;
        private readonly IReadOnlyDictionary<Rule, string> _ruleOrigins;
        private EvaluationType _activeEvaluationType;
        private readonly RuleEvaluateResult _evalResult = new() { StopConditionValidated = false };
        private TCompareExpression _query;

        public RuleEngine(RuleContainer ruleContainer, IReadOnlyDictionary<Rule, string> ruleOrigins = null)
        {
            _ruleContainer = ruleContainer;
            _ruleOrigins = ruleOrigins;
        }

        private string OriginOf(Rule rule) =>
            _ruleOrigins != null && _ruleOrigins.TryGetValue(rule, out var name) ? name : _ruleContainer.Name;

        /// <remarks>
        /// Minimal 1 and maximal all rules are evaluated once, from "top" to "bottom".
        /// Rules can be ordered implicitly (literally from top to bottom) or via their priorities.
        /// Inbetween breaks can be set in two ways (FUTURE use):
        ///  - "tactical measure": stop and leave current container (by rule.Stop, only if the rule fired)
        ///  - "strategical measure": request to stop and leave the whole process (by container.StopRequested)
        /// </remarks>
        public RuleEvaluateResult EvaluateOnce(ValueBag context)
        {
            _activeEvaluationType = EvaluationType.FireOnce;
            using var resolution = context.MessageSink.UseResolver(DeriveValue);
            _ruleContainer.Rules.Sort(); // The container and its rules belong to this evaluation, not a shared cache.

            foreach (Rule rule in _ruleContainer.Rules)
            {
                try
                {
                    if (rule.Evaluate(context))
                    {
                        RuleTrace ruleTrace = _evalResult.AddOrGetRule(rule);
                        if (rule.Stop)
                        {
                            break;
                        }
                    }
                }
                catch (TExprException ee)
                {
                    throw new TExprException($"{ee.Message}. Source is rule {rule.Name} in container {_ruleContainer.Name}.");
                }
                catch (Exception)
                {
                    throw;
                }
            }

            _evalResult.StopConditionValidated = _evalResult.RuleGotFired && (_ruleContainer.StopConditionExpr?.Evaluate(context) == true);

            return _evalResult;
        }

        /// <remarks>
        /// Retry rules whose conditions have not yet matched (forward chaining).
        /// Each rule can fire at most once during this call. Already-fired rules are
        /// skipped entirely, including their conditions and conclusions.
        /// Iteration ends after a full pass in which no new rule fires.
        /// Rules are visited in container order; conflicting conclusions can still depend on that order.
        /// Inbetween break can be set in one way:
        ///  - "strategical measure": request to stop and leave the whole process (by container.StopRequested)
        /// </remarks>
        public RuleEvaluateResult EvaluateByIteration(ValueBag context)
        {
            _activeEvaluationType = EvaluationType.Iteration;
            using var resolution = context.MessageSink.UseResolver(DeriveValue);
            var firedRules = new HashSet<Rule>(ReferenceEqualityComparer.Instance);
            bool ruleGotFired = true;
            while (ruleGotFired)
            {
                ruleGotFired = false;
                foreach (Rule rule in _ruleContainer.Rules)
                {
                    if (firedRules.Contains(rule)) continue;
                    try
                    {
                        if (rule.Evaluate(context))
                        {
                            firedRules.Add(rule);
                            RuleTrace ruleTrace = _evalResult.AddOrGetRule(rule);
                            ruleGotFired = true;
                        }
                    }
                    catch (TExprException ee)
                    {
                        throw new TExprException($"{ee.Message}. Source is rule {rule.Name} in container {_ruleContainer.Name}.");
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }

            _evalResult.StopConditionValidated = _evalResult.RuleGotFired && (_ruleContainer.StopConditionExpr?.Evaluate(context) == true);

            return _evalResult;
        }

        /// <summary>
        /// Start with a possible conclusion A, work backwards by mapping rule A's conditions on rule B,C,D, etc. their conclusions
        /// This happens via the expression itself and our delegate DeriveValue, this method is just the kickstarter
        /// </summary>
        /// <param name="query">A <see cref="TCompareExpression"/> to be resolved</param>
        /// <param name="context">The <see cref="ValueBag"/> specific to the user asking the query</param>
        /// <param name="result">A trace of fired rules ledingto the final conclusion</param>
        /// <returns>True if the query could be resolved otherwise false</returns>
        public RuleEvaluateResult EvaluateByDeduction(TCompareExpression query, ValueBag context)
        {
            _activeEvaluationType = EvaluationType.Deduction;
            ArgumentNullException.ThrowIfNull(query);
            using var resolution = context.MessageSink.UseResolver(DeriveValue);
            Debug.Assert(query != null, "Cannot invoke evaluation by deduction if there is no query");
            var previousProof = context.ProofRecorder;
            var recorder = new ProofRecorder(_ruleContainer.Name);
            var root = recorder.Begin(ProofNodeKind.Query, query.ToString());
            context.ProofRecorder = recorder;
            _query = query;
            try
            {
                _evalResult.ProvenByInduction = _query.Evaluate(context);
                root.Complete(_evalResult.ProvenByInduction.Value);
            }
            catch(TExprException ee)
            {
                root.State(ee.CouldNotDetermineIdValue() ? ProofState.Unknown : ProofState.Error);
                if (ee.CouldNotDetermineRootIdValue(query))
                {
                    // This was the question asked by the user! So the question could not be proven as a statement.
                    _evalResult.ProvenByInduction = false;
                }
                else if (ee.CouldNotDetermineIdValue())
                {
                    // A property value could not be determined, so rules are missing or user did not supply data 
                    _evalResult.ProvenByInduction = null;
                }
                else
                {
                    throw;
                }
            }
            catch (Exception)
            {
                root.State(ProofState.Error);
                throw;
            }
            finally
            {
                root.Dispose();
                context.ProofRecorder = previousProof;
                _evalResult.Proof = recorder.Snapshot();
            }
            return _evalResult;
        }

        private List<Rule> GetRulesForQuery(TId value)
        {
            return _ruleContainer.Rules.FindAll(r => r.Conclusions.Exists(c => c.LeftExpr.Equals(value.Name)));
        }

        /// <summary>
        /// Given a <see cref="TId"/> and a context, determine its value. 
        /// This can happen via a recursive process (backward chaining) or simply by asking the user 
        /// </summary>
        /// <param name="subject">a <see cref="TConstantValue"/> which value we need to determine</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private TConstantValue DeriveValue(TId subject, ValueBag context)
        {
            Debug.WriteLine($"Have to derive value of {subject.Name}...");
            if (_activeEvaluationType == EvaluationType.Deduction)
            {
                // Is it the query itself? (non-boolean) If so, we cannot derive it, because it is the question asked by the user!
                //@@if (subject.Equals(_query.LeftExpr.Terms[0].Factors[0]) && _query.RightExpr is not null) return null; // TODO: derive a TQuery from TCompareExpression (with this prop added)

                List<Rule> rules = GetRulesForQuery(subject);
                if (rules.Count == 0)
                {
                    using var input = context.ProofRecorder?.Begin(ProofNodeKind.Evidence, subject.Name);
                    input?.Source(EvidenceSource.UserAnswer);
                    input?.State(ProofState.WaitingForInput);
                    var answer = context.MessageSink.GetUserInput?.Invoke(subject, context);
                    input?.Complete(answer);
                    if (answer != null) context.ProofRecorder?.Remember(subject.Name, EvidenceSource.UserAnswer);
                    return answer;
                }
                else
                {
                    int i = 0;
                    bool proven = false;
                    while (!proven && i < rules.Count)
                    {
                        Rule rule = rules[i++];
                        using var attempt = context.ProofRecorder?.Begin(ProofNodeKind.Rule, $"{OriginOf(rule)}/{rule.Name}");
                        proven = true;
                        foreach (TCompareExpression c in rule.Conditions)
                        {
                            using var condition = context.ProofRecorder?.Begin(ProofNodeKind.Condition, c.ToString());
                            if (!proven)
                            {
                                condition?.State(ProofState.Skipped);
                                continue;
                            }
                            proven = c.Evaluate(context);
                            condition?.Complete(proven);
                        }
                        if (proven)
                        {
                            rule.AssertConclusions(context);
                            _evalResult.AddOrGetRule(rule, _ruleOrigins == null ? null : OriginOf(rule));
                        }
                        attempt?.Complete(proven);
                    }
                    return proven ? context.GetValue(subject.Name) : null;
                }
            }
            else
            {
                return context.MessageSink.GetUserInput?.Invoke(subject, context);
            }
        }
    }
}
