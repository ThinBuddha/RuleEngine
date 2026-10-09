using System;
using System.Collections.Generic;
using System.Globalization;
using Rules.Model;

namespace Rules.Parser
{
    public sealed class ModelBuilder : Builder
    {
        private readonly Stack<ParsedItem> _scope = new();
        private string _name, _base, _short, _property, _reference, _atomic, _classReference;
        private string _comparison;
        private ModelBound _minimum, _maximum;
        private List<string> _values;
        public ExplicitModel Model { get; } = new();
        public List<string> Errors { get; } = [];

        public ModelBuilder(ParseTree tree)
        {
            tree.OnStart = Start;
            tree.OnAdd = Add;
            tree.OnEnd = End;
            Result = Model;
        }

        private void Start(ParsedItem item, string _)
        {
            _scope.Push(item);
            if (item is ParsedItem.UnitDecl or ParsedItem.TypeDecl or ParsedItem.ClassDecl)
            {
                _name = _base = _short = null;
                _minimum = _maximum = null;
                _values = [];
            }
            if (item == ParsedItem.Property) _property = _reference = _atomic = _classReference = null;
            if (item is ParsedItem.Min or ParsedItem.Max) _comparison = null;
        }

        private void Add(ParsedItem item, string value)
        {
            var scope = _scope.Peek();
            switch (item)
            {
                case ParsedItem.Id:
                    if (scope == ParsedItem.Property) _property = value;
                    else
                    {
                        _name = value;
                        if (scope == ParsedItem.ClassDecl) _currentClass = new ModelClass(value);
                    }
                    break;
                case ParsedItem.BaseType: _base = value; break;
                case ParsedItem.Short: _short = value; break;
                case ParsedItem.TypeRef: _reference = value; break;
                case ParsedItem.ClassRef: _classReference = value; break;
                case ParsedItem.AtomicType: _atomic = value; break;
                case ParsedItem.String: _values.Add(value); break;
                case ParsedItem.Compare: _comparison = value; break;
                case ParsedItem.Integer:
                case ParsedItem.Float:
                    var bound = new ModelBound(double.Parse(value, CultureInfo.InvariantCulture), _comparison == "=", item == ParsedItem.Integer);
                    if (scope == ParsedItem.Min) _minimum = bound;
                    else if (scope == ParsedItem.Max) _maximum = bound;
                    break;
            }
        }

        private void End()
        {
            switch (_scope.Pop())
            {
                case ParsedItem.UnitDecl:
                    if (_name != null && _base != null && _short != null && !Model.Units.TryAdd(_name, new ModelUnit(_name, _base, _short)))
                        Errors.Add($"Duplicate unit '{_name}'");
                    break;
                case ParsedItem.TypeDecl:
                    if (_name != null && _base != null)
                    {
                        var type = new ModelType(_name, _base) { Minimum = _minimum, Maximum = _maximum };
                        type.Values.AddRange(_values);
                        if (!Model.Types.TryAdd(_name, type)) Errors.Add($"Duplicate type '{_name}'");
                    }
                    break;
                case ParsedItem.ClassDecl:
                    if (_name != null && !Model.Classes.ContainsKey(_name)) Model.Classes.Add(_name, _currentClass);
                    else if (_name != null) Errors.Add($"Duplicate class '{_name}'");
                    _currentClass = null;
                    break;
                case ParsedItem.Property:
                    if (_property != null && _currentClass != null &&
                        !_currentClass.Properties.TryAdd(_property, new ModelProperty(_property, _reference ?? _atomic, _classReference, _atomic)))
                        Errors.Add($"Duplicate property '{_property}' in class '{_currentClass.Name}'");
                    break;
            }
        }

        private ModelClass _currentClass;
    }
}
