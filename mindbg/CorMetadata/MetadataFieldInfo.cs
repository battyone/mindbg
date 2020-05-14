using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using MinDbg.NativeApi;

namespace MinDbg.CorMetadata
{
    internal sealed class MetadataFieldInfo : FieldInfo
    {
        private readonly IMetadataImport _importer;

        internal MetadataFieldInfo(IMetadataImport importer, Int32 fieldToken)
        {
            _importer = importer;

            importer.GetFieldProps(fieldToken, out var fieldTypeDef, null, 0, out var size,
                out _, out _, out _, out _, out _, out _);

            StringBuilder szFieldName = new StringBuilder(size);
            importer.GetFieldProps(fieldToken, out _, szFieldName, szFieldName.Capacity, out _,
                out _, out _, out _, out _, out _, out _);

            Name = szFieldName.ToString();
            MetadataToken = fieldToken;
        }

        public override string Name { get; }

        public override int MetadataToken { get; }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object GetValue(object obj)
        {
            throw new NotImplementedException();
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override Type DeclaringType { get; }
        public override Type ReflectedType { get; }
        public override Type FieldType { get; }
        public override RuntimeFieldHandle FieldHandle { get; }
        public override FieldAttributes Attributes { get; }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
