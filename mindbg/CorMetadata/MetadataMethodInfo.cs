using System;
using System.Globalization;
using System.Reflection;
using System.Text;
using MinDbg.NativeApi;

namespace MinDbg.CorMetadata
{
    internal sealed class MetadataMethodInfo : MethodInfo
    {
        private readonly Int32 p_classToken;
        private readonly IMetadataImport p_importer;

        internal MetadataMethodInfo(IMetadataImport importer, Int32 methodToken)
        {
            p_importer = importer;
            MetadataToken = methodToken;

            p_importer.GetMethodProps((uint)methodToken,
                                      out p_classToken,
                                      null,
                                      0,
                                      out var size,
                                      out _,
                                      out _,
                                      out _,
                                      out _,
                                      out _);

            StringBuilder szMethodName = new StringBuilder(size);
            p_importer.GetMethodProps((uint)methodToken,
                                    out p_classToken,
                                    szMethodName,
                                    szMethodName.Capacity,
                                    out size,
                                    out _,
                                    out _,
                                    out _,
                                    out _,
                                    out _);

            Name = szMethodName.ToString();
            //m_methodAttributes = (MethodAttributes)pdwAttr;
        }

        public override string Name { get; }

        public override int MetadataToken { get; }

        public override MethodInfo GetBaseDefinition()
        {
            throw new NotImplementedException();
        }

        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

        public override MethodAttributes Attributes => throw new NotImplementedException();

        public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

        public override Type DeclaringType => throw new NotImplementedException();

        public override Type ReflectedType => throw new NotImplementedException();

        public override MethodImplAttributes GetMethodImplementationFlags()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            throw new NotImplementedException();
        }

        public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
