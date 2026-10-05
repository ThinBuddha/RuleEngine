using Rules.QueryInterpreter;
using Rules.Expression;
using Rules.Model;
using Rules.Parser;
using Rules.Rules;
using Rules.Tasks;
using System;
using System.IO;
using static Rules.Tasks.TTask;
using System.Collections.Generic;

namespace Rules.CommandShell
{
    /// <summary>
    /// Command shell to load and execute <see cref="Rule"/>s.
    /// It is a first start to test the application of <see cref="Rule"/>s and future interfaces.
    /// </summary>
    class Program
    {
        private static readonly TTask _task = new TTask();
        private static readonly ValueBag _context = new ValueBag();

        private static RuleContainers _ruleContainers = new RuleContainers();
        private static TaskEvaluateResults _evalResult;

        public static Interpreter _interpreter = new Interpreter();

        static void Main(string[] args)
        {
            // Set handler for when we receive a message to deliver user input 
            MessageSink.GetUserInput = GetUserInput;

            ShowCommands();
            string command;
            while (true)
            {
                WritePrompt();
                command = Console.ReadLine();
                if (IsEqualString(command, "quit"))
                {
                    break;
                }
                if (command.ToUpper().StartsWith("LR"))
                {
                    LoadRulesFile(command);
                }
                else
                {
                    // Just a command and then enter?
                    switch (command)
                    {
                        case "er": ExecuteRules(); break;
                        case "rt": ShowRuleTrace(); break;
                        case "sm": ShowSummary(); break;
                        case "cn": ShowContents(); break;
                        case "cc": ClearContext(); break;
                        case "cr": ClearRuleContainers(); break;
                        case "calc": EvaluateExpression(); break;
                        case "help": ShowCommands(); break;
                    }
                    // Or instead a command an parameters on one line
                    if (command.ToUpper().StartsWith("CALC "))
                    {
                        EvaluateExpression( command.Substring(5, command.Length - 5) );
                    }
                }
            }
        }

        private static void EvaluateExpression(string commandParams = "")
        {
            // Set handler for when we receive a notification that we've got to deal with an unkown value
            MessageSink.NotifyUnknownValue = DeriveValue;

            string input = string.IsNullOrEmpty(commandParams) ? Console.ReadLine() : commandParams;
            RuleParser parser = new RuleParser(RulePart.Expression);
            parser.Parse(input);
            var expr = parser.Builder.Result as TExpression;
            try
            {
                TConstantValue result = expr.Evaluate(_context);
                Console.WriteLine($"Result: {result}");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Invalid expression: {ex.Message}.");
            }
        }

        private static TConstantValue DeriveValue(TId subject, ValueBag context)
        {
            // We need to determine what the unknown value is
            // F.t.t.b. we are not so smart and delegate it to the user
            return MessageSink.GetUserInput(subject, context);
        }

        private static void ClearContext()
        {
            _context.Clear();
            Console.WriteLine("The rule context was cleared");
        }

        private static void ClearRuleContainers()
        {
            _task.Clear();
            _ruleContainers.Clear();
            Console.WriteLine("All rules in all rule containers were cleared");
        }

        static private TConstantValue GetUserInput(TId subject, ValueBag context)
        {
            // This works quite allright for a simple and monolitic app
            // In future versions the rule logic will be behind API's and subject will be a json string
            if (subject.GetType() == typeof(TId))
            {
                Console.Write($"{((TId)subject).Name}: ");
                string answer = Console.ReadLine();
                if (IsEqualString(answer, "yes") || IsEqualString(answer, "y") || IsEqualString(answer, "true"))
                {
                    return new TConstantNumValue(true);
                }
                else if (IsEqualString(answer, "no") || IsEqualString(answer, "n") || IsEqualString(answer, "false"))
                {
                    return new TConstantNumValue(false);
                }
                else if (int.TryParse(answer, out int intValue))
                {
                    return new TConstantNumValue(intValue);
                }
                else if (double.TryParse(answer, out double floatValue))
                {
                    return new TConstantNumValue(floatValue);
                }
                else
                {
                    return new TConstantStringValue(answer);
                }
            }
            return null;
        }

        private static void LoadRulesFile(string command)
        {
            string file;
            if (IsEqualString("lr", command))
            {
                Console.WriteLine("A map to look for files might be this one: ");
                Console.WriteLine("..\\..\\..\\RuleEngine\\RuleParser\\doc\\Examples");
                Console.Write("Give the name of the file: ");
                file = Console.ReadLine();
            }
            else
            {
                file = command[3..];
            }
            Console.WriteLine("Choose the type of evaluation: 0 = Once, 1 = Iterate, 2 = Deduction (or anything else to use the rule container's default): ");
            EvaluationType evaluationType;
            bool useContainerEvalTypes = false;
            var choice = Console.ReadKey();
            switch (choice.Key)
            { 
                case ConsoleKey.D0: evaluationType = EvaluationType.FireOnce; break;
                case ConsoleKey.Enter: evaluationType = EvaluationType.FireOnce; break;
                case ConsoleKey.D1: evaluationType = EvaluationType.Iteration; break;
                case ConsoleKey.D2: evaluationType = EvaluationType.Deduction; break;
                default: evaluationType = EvaluationType.FireOnce; useContainerEvalTypes = true; break;
            }

            try
            {
                string[] content = File.ReadAllLines(file);
                RuleParser parser = new RuleParser(RulePart.RuleContainer);
                parser.Parse(string.Join(Environment.NewLine, content));
                if (parser.SyntaxErrors.Count > 0)
                {
                    Console.WriteLine($"There are {parser.SyntaxErrors.Count} syntax errors in the file {file}");
                    Console.WriteLine($"The first {Math.Min(parser.SyntaxErrors.Count, 5)} errors are:");
                    for (int i = 0; i < 5; i++)
                    {
                        Console.WriteLine(parser.SyntaxErrors[i].Item1);
                    }
                }
                else
                {
                    Console.WriteLine("Successfully loaded file");
                    _ruleContainers = (parser.Builder as RulesBuilder).RuleContainers;
                    if (!useContainerEvalTypes)
                    {
                        _ruleContainers.Containers.ForEach(c => c.EvaluationType = evaluationType);
                    }
                    if (_ruleContainers.Containers.FindAll(c => c.EvaluationType == EvaluationType.Deduction).Count > 1)
                    {
                        Console.WriteLine("Cannot apply a query to more then one rule container");
                        _ruleContainers.Clear();
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Problem while reading file: {e.Message}");
            }
        }

        private static void ShowRuleTrace()
        {
            Console.WriteLine("");
            Console.WriteLine("Facts:");
            Console.WriteLine("------");
            Console.WriteLine(_context.ToString());
            Console.WriteLine("");
            if (_evalResult != null)
            {
                Console.WriteLine("Rationale:");
                Console.WriteLine("----------");
                Console.WriteLine(_evalResult.ToString());
            }
        }

        private static void ExecuteRules()
        {
            // TODO if -cc then _context.Clear();
            // TODO if -m <mode> then overrule the mode defined in the Rulecontainer
            _task.RuleContainers = _ruleContainers;
            try
            {
                if (_task.RuleContainers.Containers.Count == 0)
                {
                    throw new Exception("There are no rules loaded, cannot evaluate.");
                }
                string query = string.Empty;
                TCompareExpression compareExpr = null;
                if (_task.RuleContainers.Containers[0].EvaluationType == EvaluationType.Deduction)
                {
                    query = ReadQuery();
                    QueryToExpression(query, out compareExpr, out string errors);
                    while (!string.IsNullOrEmpty(errors))
                    {
                        Console.WriteLine("Syntax error in the query, please try again");
                        query = ReadQuery();
                        QueryToExpression(query, out compareExpr, out errors);
                    }
                }
                _evalResult = _task.Evaluate(_context, compareExpr);
                if (_evalResult.QueryResult != null)
                {
                    Console.WriteLine($"The answer to your query is: {_evalResult.QueryResult}");
                }
                else
                {
                    Console.WriteLine($"Your query could not be answered, due to missing rules or lack of user answers...");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            ShowRuleTrace();
        }

        private static void QueryToExpression(string query, out TCompareExpression compareExpr, out string errors)
        {
            List<Tuple<string, string>> errorList;
            errors = string.Empty;

            (compareExpr, errorList) = _interpreter.QueryToExpression(query);
            if (errorList != null)
            {
                foreach (Tuple<string, string> e in errorList)
                {
                    errors += $"{e.Item1},{e.Item2}" + Environment.NewLine;
                }
            }
        }

        private static string ReadQuery()
        {
            string query;
            {
                Console.WriteLine("Type your query: ");
                query = Console.ReadLine();
            } while (string.IsNullOrEmpty(query));
            return query;
        }

        private static void ShowContents()
        {
           Console.WriteLine(_ruleContainers.ToString());
        }

        private static void ShowSummary()
        {
            Console.WriteLine($"File contains {_ruleContainers.Containers.Count} rule containers");
            foreach( RuleContainer container in _ruleContainers.Containers)
            {
                Console.WriteLine($"Container {container.Name} contains {container.Rules.Count} rules");
            }
        }

        private static void ShowCommands()
        {
            Console.WriteLine("Use any of the following commands:");
            Console.WriteLine("lr <filename> -cr\tto load a file with rules; clear current rules if -cr");
            Console.WriteLine("er [-m <mode>] [-cc]\tto execute currently loaded rules in the given mode; clear context if -cc");
            Console.WriteLine("sm\t\t\tto show a summary of the currently loaded file contents");
            Console.WriteLine("cn\t\t\tto show the contents of the currently loaded file");
            Console.WriteLine("rt\t\t\tto show a trace of the last executed rules");
            Console.WriteLine("cc\t\t\tto clear the current context (proven facts)");
            Console.WriteLine("cr\t\t\tto clear the current rules");
            Console.WriteLine("calc\t\t\tto calculate a number with an expression");
            Console.WriteLine("help\t\t\tto show this help text");
            Console.WriteLine("quit\t\t\tto sign off and close the command shell");
            Console.WriteLine("");
        }

        private static bool IsEqualString(string value1, string value2) =>
            string.Compare(value1, value2, StringComparison.OrdinalIgnoreCase) == 0;

        private static void WritePrompt()
        {
            Console.Write(":> ");
        }
    }
}
