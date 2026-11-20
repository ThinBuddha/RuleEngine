using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Rules.Rules;

namespace Rules.Model
{
    /// <summary>
    /// A node in the hierarchy formed by dotted fact IDs.
    /// In other words: a model inferred from the rules, not explicitly defined beforehand (<see cref="ExplicitModel"/> for that).
    /// </summary>
    public class DerivedModelClass
    {
        private readonly SortedDictionary<string, DerivedModelClass> _classes = new(StringComparer.Ordinal);
        private readonly SortedDictionary<string, DerivedModelProperty> _properties = new(StringComparer.Ordinal);
        private readonly IReadOnlyDictionary<string, DerivedModelClass> _classesView;
        private readonly IReadOnlyDictionary<string, DerivedModelProperty> _propertiesView;

        internal DerivedModelClass(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
            _classesView = new ReadOnlyDictionary<string, DerivedModelClass>(_classes);
            _propertiesView = new ReadOnlyDictionary<string, DerivedModelProperty>(_properties);
        }

        /// <summary>The final segment of this class's dotted path. Empty for the root.</summary>
        public string Name { get; }
        /// <summary>The dotted path from the root. Empty for the root.</summary>
        public string FullName { get; }
        public IReadOnlyDictionary<string, DerivedModelClass> Classes => _classesView;
        public IReadOnlyDictionary<string, DerivedModelProperty> Properties => _propertiesView;

        internal DerivedModelClass GetOrAddClass(string name)
        {
            if (!_classes.TryGetValue(name, out var modelClass))
            {
                var fullName = FullName.Length == 0 ? name : $"{FullName}.{name}";
                _classes.Add(name, modelClass = new DerivedModelClass(name, fullName));
            }
            return modelClass;
        }

        internal void AddProperty(DerivedModelProperty property) => _properties.Add(property.Name, property);
    }

    /// <summary>A fact identified by a rule condition or conclusion.</summary>
    public class DerivedModelProperty
    {
        internal DerivedModelProperty(string name, string fullName, string kind, IReadOnlyList<string> stringValues)
        {
            Name = name;
            FullName = fullName;
            Kind = kind;
            StringValues = Array.AsReadOnly(stringValues.ToArray());
        }

        public string Name { get; }
        public string FullName { get; }
        /// <summary>"Boolean", "numeric", "string", or null if unconstrained.</summary>
        public string Kind { get; }
        public IReadOnlyList<string> StringValues { get; }
    }

    public class DerivedModelDeriver
    {
        /// <summary>Builds a tree from the fact IDs and types inferred from the rules.</summary>
        public static DerivedModelClass Derive(RuleContainers containers)
        {
            ArgumentNullException.ThrowIfNull(containers);
            return Derive(DerivedModelTypes.Derive(containers));
        }

        /// <summary>
        /// Builds a tree from an existing type derivation.
        /// </summary>
        /// <remarks>
        /// This makes use of the derived type's name's class-property structure, 
        /// so its use is limited to rules that don't enforce the usage of an explicit model.
        /// </remarks>
        public static DerivedModelClass Derive(DerivedModelTypes types)
        {
            ArgumentNullException.ThrowIfNull(types);
            var root = new DerivedModelClass(string.Empty, string.Empty);

            foreach (var id in types.Identifiers.OrderBy(id => id, StringComparer.Ordinal))
            {
                var parts = id.Split('.');
                var owner = root;
                for (var i = 0; i < parts.Length - 1; i++)
                    owner = owner.GetOrAddClass(parts[i]);

                owner.AddProperty(new DerivedModelProperty(parts[^1], id,
                    types.GetKind(id), types.GetStringValues(id)));
            }

            return root;
        }
    }
}
