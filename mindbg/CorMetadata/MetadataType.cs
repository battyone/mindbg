using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using MinDbg.NativeApi;

namespace MinDbg.CorMetadata
{
    internal sealed class MetadataType : Type
    {
        private readonly IMetadataImport _importer;
        private readonly Int32 _typeToken;

        internal MetadataType(IMetadataImport importer, Int32 typeToken)
        {
            _importer = importer;
            _typeToken = typeToken;

            var nameBuilder = new StringBuilder(256);
            importer.GetTypeDefProps(typeToken, nameBuilder, 256, out var length, out _, out _);
            Name = nameBuilder.ToString();
            MetadataToken = typeToken;
        }

        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            IntPtr hEnum = new IntPtr();
            var methods = new List<MethodInfo>();

            try
            {
                while (true)
                {
                    _importer.EnumMethods(ref hEnum, _typeToken, out var methodToken, 1, out var size);
                    if (size == 0)
                        break;
                    methods.Add(new MetadataMethodInfo(_importer, methodToken));
                }
            }
            finally
            {
                _importer.CloseEnum(hEnum);
            }
            return methods.ToArray();
        }

        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            IntPtr hEnum = new IntPtr();
            var fields = new List<FieldInfo>();

            try
            {
                while (true)
                {
                    _importer.EnumFields(ref hEnum, _typeToken, out var fieldToken, 1, out var size);
                    if (size == 0)
                        break;
                    fields.Add(new MetadataFieldInfo(_importer, fieldToken));
                }
            }
            finally
            {
                _importer.CloseEnum(hEnum);
            }
            return fields.ToArray();
        }

        public override string Name { get; }

        public override int MetadataToken { get; }

        public override Assembly Assembly => throw new NotImplementedException();

        public override string AssemblyQualifiedName => throw new NotImplementedException();

        public override Type BaseType => throw new NotImplementedException();

        public override string FullName => throw new NotImplementedException();

        public override Guid GUID => throw new NotImplementedException();

        public override Module Module => throw new NotImplementedException();

        public override string Namespace => throw new NotImplementedException();

        public override Type UnderlyingSystemType => throw new NotImplementedException();

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            throw new NotImplementedException();
        }

        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type GetElementType()
        {
            throw new NotImplementedException();
        }

        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type GetInterface(string name, bool ignoreCase)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetInterfaces()
        {
            throw new NotImplementedException();
        }

        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public override Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        protected override bool HasElementTypeImpl()
        {
            throw new NotImplementedException();
        }

        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new NotImplementedException();
        }

        protected override bool IsArrayImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsByRefImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsCOMObjectImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsPointerImpl()
        {
            throw new NotImplementedException();
        }

        protected override bool IsPrimitiveImpl()
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
