using Rules;
using Rules.Expression;
using Rules.Model;
using Rules.Parser;
using Rules.QueryInterpreter;
using Rules.Rules;
using Rules.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace RuleTest
{
    /* Total test matrix for deduction per query type (multiple rules yes/no, true yes/no, false yes/no, unknown yes/no):
       Type         Query type	Multiple rules	True	False	Unknown	
       Deduction    A > 5		no			    yes	    no	    no
		                        no			    no	    yes	    no
		                        no			    no	    no	    yes
		                        yes			    yes	    no	    no
		                        yes			    no	    yes	    no
		                        yes			    no	    no	    yes

                   A.is.big	    no			    yes	    no	    no
		                        no			    no	    yes	    no
		                        no			    no	    no	    yes
		                        yes			    yes	    no	    no
		                        yes			    no	    yes	    no
		                        yes			    no	    no	    yes
     */
    public class TestRuleEngineDeduction

    {
        private readonly Interpreter _interpreter = new Interpreter();
        private readonly ValueBag _context = new ValueBag();
        private readonly UserAnswer _userAnswer = new UserAnswer();

        #region Simple tests
        [Fact]
        public void TestDeductionMultipleRules_NoChaining()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

            // Mock user answers
            _userAnswer.AddAnswer("Outside.it.rains", true);
            _userAnswer.AddAnswer("Person.has.umbrella", true);
            _userAnswer.AddAnswer("Stamina.of.person", "great");
            _userAnswer.AddAnswer("Distance.to.travel", "medium");

            // Setup the rules
            RuleContainers ruleContainers = TestHelper.GetTravelRules();

            // Setup the query
            string query = "Travel.by.walking";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rule
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, EvaluationType.Deduction, compareExpr);
            Assert.NotNull(evalResult);
            Assert.NotNull(evalResult.QueryResult);
            Assert.True(evalResult.QueryResult);

            // TODO: inspect traces by code, not by string output
            StringBuilder result = new StringBuilder()
                .AppendLine("There were 1 tasks executed.")
                .AppendLine("")
                .AppendLine("RuleContainer \"Determine.how.to.travel\" says:")
                .AppendLine("\t1 rules were applied")
                .AppendLine("\tStop condition validated: False")
                .AppendLine("")
                .AppendLine("\tRule \"walking.4\" says:")
                .AppendLine("\t\tTravel.by.walking")
                .AppendLine("\tBecause:")
                .AppendLine("\t\tOutside.it.rains")
                .AppendLine("\t\tDistance.to.travel = \"medium\"")
                .AppendLine("\t\tPerson.has.umbrella")
                .AppendLine("\t\tStamina.of.person = \"great\"")
                .AppendLine("")
                .AppendLine("Request to halt process was False");

            Assert.True(evalResult.ToString() == result.ToString(), "Evaluated result description was not as expected");
        }
        #endregion


        #region Exceptional cases
        [Fact]
        public void TestDeductionFinalRuleMissing()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

            // Mock user answers
            _userAnswer.AddAnswer("App.Needed", "database");
            _userAnswer.AddAnswer("Security.Level", "low"); // there is no rule for this, so Discs.MinNumber should remain undefined

            // Setup the rule
            string input = "RuleContainer System.Configuration Rule Discs.Minimal.2 If App.Needed = \"database\" and Security.Level = \"high\" then Discs.MinNumber := 2";
            RuleContainers ruleContainers = TestHelper.ParseRules(input);
            Assert.Single(ruleContainers.Containers);

            // Setup the query
            string query = "Discs.MinNumber >= 2";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out string _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rule
            var task = new TTask { RuleContainers = ruleContainers };
            try
            {
                var evalResult = task.Evaluate(_context, EvaluationType.Deduction, compareExpr);
                Assert.Null(evalResult.QueryResult);
            }
            //catch(Xunit.Sdk.AssertActualExpectedException xe)
            //{

            //}
            catch (Exception ex)
            {
                Assert.True(false, $"Exception was raised but did not expect this to happen. Message: {ex.Message}");
            }
        }

        [Fact]
        public void TestDeductionIntermediateRuleMissing()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

            // Mock user answers, not all answers will return a value (Security.Level remains unknown)
            _userAnswer.AddAnswer("App.Needed", "database");

            // Setup the rule
            string input = "RuleContainer System.Configuration Rule Discs.Minimal.2 If App.Needed = \"database\" and Security.Level = \"high\" then Discs.MinNumber := 2";
            RuleContainers ruleContainers = TestHelper.ParseRules(input);
            Assert.Single(ruleContainers.Containers);

            // Setup the query
            string query = "Discs.MinNumber >= 2";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out string _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rule
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, EvaluationType.Deduction, compareExpr);
            Assert.NotNull(evalResult);
            Assert.Null(evalResult.QueryResult);
        }
        #endregion


        #region Compare expression
        [Fact]
        public void TestDeduction_CompExpr_0100()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

            // Mock user answers
            _userAnswer.AddAnswer("App.Needed", "database");
            _userAnswer.AddAnswer("Security.Level", "high");

            // Setup the rule
            string input = "RuleContainer System.Configuration Rule Discs.Minimal.2 If App.Needed = \"database\" and Security.Level = \"high\" then Discs.MinNumber := 2";
            RuleContainers ruleContainers = TestHelper.ParseRules(input);
            Assert.Single(ruleContainers.Containers);

            // Setup the query
            string query = "Discs.MinNumber >= 2";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rule
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, EvaluationType.Deduction, compareExpr);
            Assert.NotNull(evalResult);
            Assert.NotNull(evalResult.QueryResult);
            Assert.True(evalResult.QueryResult);
        }

        [Fact]
        public void TestDeduction_CompExpr_0010()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

            // Mock user answers
            _userAnswer.AddAnswer("App.Needed", "database");
            _userAnswer.AddAnswer("Security.Level", "high");

            // Setup the rule
            string input = "RuleContainer System.Configuration Rule Discs.Minimal.2 If App.Needed = \"database\" and Security.Level = \"high\" then Discs.MinNumber := 2";
            RuleContainers ruleContainers = TestHelper.ParseRules(input);
            Assert.Single(ruleContainers.Containers);

            // Setup the query
            string query = "Discs.MinNumber < 2";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rule
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, EvaluationType.Deduction, compareExpr);
            Assert.NotNull(evalResult);
            Assert.NotNull(evalResult.QueryResult);

            // Discs.MinNumber should have been proven to be 2, so our query answer should be "no" (false)
            Assert.False(evalResult.QueryResult);
        }

        [Fact]
        public void TestDeduction_CompExpr_0001()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

            // Mock user answers (don't answer all questions)
            _userAnswer.AddAnswer("App.Needed", "database");

            // Setup the rule
            string input = "RuleContainer System.Configuration Rule Discs.Minimal.2 If App.Needed = \"database\" and Security.Level = \"high\" then Discs.MinNumber := 2";
            RuleContainers ruleContainers = TestHelper.ParseRules(input);
            Assert.Single(ruleContainers.Containers);

            // Setup the query
            string query = "Discs.MinNumber < 2";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rule
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, EvaluationType.Deduction, compareExpr);
            Assert.NotNull(evalResult);

            // There should be no result
            Assert.Null(evalResult.QueryResult);
        }

        [Fact]
        public void TestDeduction_CompExpr_1100()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

        }

        [Fact]
        public void TestDeduction_CompExpr_1010()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

        }

        [Fact]
        public void TestDeduction_CompExpr_1001()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

        }
        #endregion


        #region Logical expression
        [Fact]
        public void TestDeduction_LogicExpr_0100()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

            // Mock user answers
            _userAnswer.AddAnswer("Outside.it.rains", true);
            _userAnswer.AddAnswer("Stamina.of.person", "lousy");
            _userAnswer.AddAnswer("Distance.to.travel", "big");
            _userAnswer.AddAnswer("Person.has.car", true);
            _userAnswer.AddAnswer("Travel.country", "Bulgaria");

            // Setup the rules
            RuleContainers ruleContainers = TestHelper.GetTravelRules();

            // Setup the query
            string query = "Travel.by.car";
            TestHelper.QueryToExpression(query, _interpreter, out TCompareExpression compareExpr, out _);
            Assert.NotNull(compareExpr);

            // Answer the query via deduction over the rules
            var task = new TTask { RuleContainers = ruleContainers };
            var evalResult = task.Evaluate(_context, EvaluationType.Deduction, compareExpr);
            Assert.NotNull(evalResult);
            Assert.NotNull(evalResult.QueryResult);
            Assert.True(evalResult.QueryResult);

            // TODO: inspect Traces

            // TODO: also test with Travel.country = "Netherlands" user answer, this way we test pass through to user answer instead of deriving Price.of.gas
        }

        [Fact]
        public void TestDeduction_LogicExpr_0010()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

        }

        [Fact]
        public void TestDeduction_LogicExpr_0001()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

        }

        [Fact]
        public void TestDeduction_LogicExpr_1100()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

        }

        [Fact]
        public void TestDeduction_LogicExpr_1010()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

        }

        [Fact]
        public void TestDeduction_LogicExpr_1001()
        {
            _context.Clear();
            _userAnswer.Initialize(_context);

        }
        #endregion
    }
}
