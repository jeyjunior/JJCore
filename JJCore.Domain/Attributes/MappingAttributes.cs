namespace JJCore.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; }

        public ColumnAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NotMappedAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class UserCodeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class UserIdAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class EntityAttribute : Attribute
    {
        public string? TableName { get; }

        public EntityAttribute(string? tableName = null)
        {
            TableName = tableName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class EditableAttribute : Attribute
    {
        public bool Enabled { get; }

        public EditableAttribute(bool enabled)
        {
            Enabled = enabled;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class StringSizeAttribute : Attribute
    {
        public int Length { get; }

        public StringSizeAttribute(int length)
        {
            Length = length;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DecimalPrecisionAttribute : Attribute
    {
        public int Precision { get; }
        public int Scale { get; }

        public DecimalPrecisionAttribute(int precision, int scale)
        {
            Precision = precision;
            Scale = scale;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RelationshipAttribute : Attribute
    {
        public string Table { get; }
        public string PrimaryKey { get; }

        public RelationshipAttribute(string table, string primaryKey = "Id")
        {
            Table = table;
            PrimaryKey = primaryKey;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RequiredFieldAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class GlyphCodeAttribute : Attribute
    {
        public string Glyph { get; }
        public string FontFamily { get; }

        public GlyphCodeAttribute(string glyph, string fontFamily)
        {
            Glyph = glyph;
            FontFamily = fontFamily;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class FontFamilyAttribute : Attribute
    {
        public string FontFamily { get; }

        public FontFamilyAttribute(string fontFamily)
        {
            FontFamily = fontFamily;
        }
    }
}
