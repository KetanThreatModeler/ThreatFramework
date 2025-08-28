using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreatFramework.Core.Domain
{
    public enum EntityKind { Component, Property, PropertyOption, Threat, SecurityRequirement, TestCase, Library }
    public static class EntityKindExt
    {
        public static string Short(this EntityKind k) => k switch
        {
            EntityKind.Component => "c",
            EntityKind.Property => "p",
            EntityKind.PropertyOption => "o",
            EntityKind.Threat => "t",
            EntityKind.SecurityRequirement => "sr",
            EntityKind.TestCase => "tc",
            EntityKind.Library => "l",
            _ => throw new ArgumentOutOfRangeException(nameof(k))
        };

        public static string Canonical(this EntityKind k) => k switch
        {
            EntityKind.Component => "component",
            EntityKind.Property => "property",
            EntityKind.PropertyOption => "property-option",
            EntityKind.Threat => "threat",
            EntityKind.SecurityRequirement => "security-requirement",
            EntityKind.TestCase => "test-case",
            EntityKind.Library => "library",
            _ => throw new ArgumentOutOfRangeException(nameof(k))
        };
    }
}
