using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace IL2CXX;

partial class DefaultBuiltin
{
    private static void SetupMemberInfo(Func<Type, Type> get, Type type, Builtin.Code code)
    {
        code.For(
            type.GetProperty(nameof(MemberInfo.DeclaringType))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__declaring_type;\n", 0)
        );
        code.For(
            type.GetMethod(nameof(MemberInfo.GetCustomAttributes), [get(typeof(bool))]),
            transpiler =>
            {
                var method = get(typeof(RuntimeCustomAttributeData)).GetMethod(nameof(RuntimeCustomAttributeData.GetAttributes)) ?? throw new Exception();
                transpiler.Enqueue(method);
                return (transpiler.GenerateCheckNull("a_0") + $"\treturn {transpiler.Escape(method)}(a_0, &t__type_of<{transpiler.Escape(transpiler.typeofAttribute)}>::v__instance, a_1);\n", 0);
            }
        );
        code.For(
            type.GetMethod(nameof(MemberInfo.GetCustomAttributes), [get(typeof(Type)), get(typeof(bool))]),
            transpiler =>
            {
                var method = get(typeof(RuntimeCustomAttributeData)).GetMethod(nameof(RuntimeCustomAttributeData.GetAttributes)) ?? throw new Exception();
                transpiler.Enqueue(method);
                return (transpiler.GenerateCheckNull("a_0") + $"\treturn {transpiler.Escape(method)}(a_0, a_1, a_2);\n", 0);
            }
        );
        code.For(
            type.GetMethod(nameof(MemberInfo.GetCustomAttributesData)),
            transpiler =>
            {
                var method = get(typeof(RuntimeCustomAttributeData)).GetMethod(nameof(RuntimeCustomAttributeData.Get)) ?? throw new Exception();
                transpiler.Enqueue(method);
                return (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__custom_attributes) {{
{'\t'}{'\t'}std::cerr << ""no custom attributes: "";
{'\t'}{'\t'}if (a_0->v__declaring_type) std::cerr << f__string(a_0->v__declaring_type->v__full_name) << ""::"";
{'\t'}{'\t'}std::cerr << '[' << f__string(a_0->{(type == transpiler.typeofRuntimeType ? "v__full_name" : "v__name")}) << ']' << std::endl;
{'\t'}}}
{'\t'}return {transpiler.Escape(method)}(a_0);
", 0);
            }
        );
        code.For(
            type.GetProperty(nameof(MemberInfo.Name))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__name);\n", 0)
        );
        code.For(
            type.GetMethod(nameof(MemberInfo.IsDefined)),
            transpiler =>
            {
                var method = get(typeof(RuntimeCustomAttributeData)).GetMethod(nameof(RuntimeCustomAttributeData.IsDefined)) ?? throw new Exception();
                transpiler.Enqueue(method);
                return ($"\treturn {transpiler.Escape(method)}(a_0, a_1, a_2);\n", 0);
            }
        );
    }
    private static (string body, int inline) GetParameters(Func<Type, Type> get, Transpiler transpiler)
    {
        var identifier = transpiler.Escape(get(typeof(ParameterInfo)));
        return (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__parameters; *p; ++p) ++n;
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(ParameterInfo[])))}, {identifier}>(n);
{'\t'}for (size_t i = 0; i < n; ++i) {{
{'\t'}{'\t'}auto q = a_0->v__parameters[i];
{'\t'}{'\t'}auto RECYCLONE__SPILL pi = f__new_zerod<{identifier}>();
{'\t'}{'\t'}pi->v_AttrsImpl = q->v__attributes;
{'\t'}{'\t'}auto t = q->v__parameter_type;
{'\t'}{'\t'}pi->v_ClassImpl = t;
{'\t'}{'\t'}if (q->v__default_value && t->f_type() == &t__type_of<t__type>::v__instance) pi->v_DefaultValueImpl = static_cast<t__type*>(t)->f_box(q->v__default_value);
{'\t'}{'\t'}pi->v_MemberImpl = a_0;
{'\t'}{'\t'}pi->v_PositionImpl = i;
{'\t'}{'\t'}p->f_data()[i] = pi;
{'\t'}}}
{'\t'}return p;
", 0);
    }
    private static void SetupMethodBase(Func<Type, Type> get, Type type, Builtin.Code code)
    {
        SetupMemberInfo(get, type, code);
        code.For(
            type.GetProperty(nameof(MethodBase.Attributes))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__attributes;\n", 0)
        );
        code.For(
            type.GetMethod(nameof(MethodBase.GetParameters)),
            transpiler => GetParameters(get, transpiler)
        );
    }
    private static Builtin SetupRuntime(this Builtin @this, Func<Type, Type> get) => @this
    .For(get(typeof(RuntimeAssembly)), (type, code) =>
    {
        code.For(
            type.GetProperty(nameof(Assembly.EntryPoint))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (a_0->v__entry_point == reinterpret_cast<t__runtime_method_info*>(-1)) throw std::runtime_error(""no entry point: "" + f__string(a_0->v__full_name));
{'\t'}return a_0->v__entry_point;
", 0)
        );
        code.For(
            type.GetProperty(nameof(Assembly.FullName))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__full_name);\n", 0)
        );
        code.For(
            type.GetMethod(nameof(Assembly.GetExportedTypes)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}std::cout << ""exported types: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__exported_types; *p; ++p) ++n;
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(Type[])))}, {transpiler.Escape(get(typeof(Type)))}>(n);
{'\t'}std::copy_n(a_0->v__exported_types, n, p->f_data());
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Assembly.GetManifestResourceStream), [get(typeof(string))]),
            transpiler =>
            {
                var type = get(typeof(UnmanagedMemoryStream));
                var constructor = type.GetConstructor([get(typeof(byte*)), get(typeof(long))]) ?? throw new Exception();
                transpiler.Enqueue(constructor);
                return (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}auto i = a_0->v__resources.find(f__string_view(a_1));
{'\t'}if (i == a_0->v__resources.end()) return nullptr;
{'\t'}auto RECYCLONE__SPILL p = f__new_zerod<{transpiler.Escape(type)}>();
{'\t'}{transpiler.Escape(constructor)}(p, i->second.first, i->second.second);
{'\t'}return p;
", 0);
            }
        );
        code.For(
            type.GetProperty(nameof(RuntimeAssembly.Name))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__name);\n", 1)
        );
    })
    .For(get(typeof(RuntimeConstructorInfo)), (type, code) =>
    {
        SetupMethodBase(get, type, code);
        code.For(
            type.GetMethod(nameof(MethodBase.Invoke), [get(typeof(BindingFlags)), get(typeof(Binder)), get(typeof(object[])), get(typeof(CultureInfo))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $"\treturn a_0->v__create(a_0, a_1, a_2, a_3 && a_3->v__length > 0 ? a_3 : nullptr, a_4);\n", 0)
        );
    })
    .For(get(typeof(RuntimeCustomAttributeData)), (type, code) =>
    {
        code.For(
            type.GetMethod(nameof(RuntimeCustomAttributeData.Get)),
            transpiler =>
            {
                string escape(ConstructorInfo x)
                {
                    transpiler.Enqueue(x);
                    return transpiler.Escape(x);
                }
                var typeofROC = get(typeof(ReadOnlyCollection<CustomAttributeTypedArgument>));
                var typeofCATA = get(typeof(CustomAttributeTypedArgument));
                var constructCATA = escape(typeofCATA.GetConstructor([transpiler.typeofType, transpiler.typeofObject]) ?? throw new Exception());
                var newCATAs = $"f__new_array<{transpiler.Escape(get(typeof(CustomAttributeTypedArgument[])))}, {transpiler.EscapeForMember(typeofCATA)}>";
                var typeofCANA = get(typeof(CustomAttributeNamedArgument));
                var typeofCAD = get(typeof(RuntimeCustomAttributeData));
                return (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}auto value = [](auto a) -> t__object*
{'\t'}{{
{'\t'}{'\t'}if (a->v_type == &t__type_of<{transpiler.Escape(transpiler.typeofString)}>::v__instance) return a->v_value ? f__new_string(static_cast<const char16_t*>(a->v_value)) : nullptr;
{'\t'}{'\t'}if (a->v_type == &t__type_of<{transpiler.Escape(transpiler.typeofType)}>::v__instance) return static_cast<t__type*>(a->v_value);
{'\t'}{'\t'}if (!a->v_type->v__array) return a->v_type->f_box(a->v_value);
{'\t'}{'\t'}auto e = a->v_type->v__element;
{'\t'}{'\t'}auto [n, p] = *static_cast<std::pair<size_t, void*>*>(a->v_value);
{'\t'}{'\t'}auto RECYCLONE__SPILL vs = {newCATAs}(n);
{'\t'}{'\t'}if (e == &t__type_of<{transpiler.Escape(transpiler.typeofString)}>::v__instance) {{
{'\t'}{'\t'}{'\t'}auto ss = static_cast<const char16_t**>(p);
{'\t'}{'\t'}{'\t'}for (size_t i = 0; i < n; ++i) {constructCATA}(&vs->f_data()[i], e, ss[i] ? f__new_string(ss[i]) : nullptr);
{'\t'}{'\t'}}} else if (e == &t__type_of<{transpiler.Escape(transpiler.typeofType)}>::v__instance) {{
{'\t'}{'\t'}{'\t'}auto ts = static_cast<t__type**>(p);
{'\t'}{'\t'}{'\t'}for (size_t i = 0; i < n; ++i) {constructCATA}(&vs->f_data()[i], e, ts[i]);
{'\t'}{'\t'}}} else {{
{'\t'}{'\t'}{'\t'}auto ps = static_cast<uint8_t*>(p);
{'\t'}{'\t'}{'\t'}for (size_t i = 0; i < n; ++i, ps += e->v__size) {constructCATA}(&vs->f_data()[i], e, e->f_box(ps));
{'\t'}{'\t'}}}
{'\t'}{'\t'}auto RECYCLONE__SPILL roc = f__new_zerod<{transpiler.Escape(typeofROC)}>();
{'\t'}{'\t'}{escape(typeofROC.GetConstructors()[0])}(roc, vs);
{'\t'}{'\t'}return roc;
{'\t'}}};
{'\t'}size_t n = 0;
{'\t'}if (auto p = a_0->v__custom_attributes) for (; *p; ++p) ++n;
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(CustomAttributeData[])))}, {transpiler.Escape(get(typeof(CustomAttributeData)))}>(n);
{'\t'}for (size_t i = 0; i < n; ++i) {{
{'\t'}{'\t'}auto ca = a_0->v__custom_attributes[i];
{'\t'}{'\t'}size_t ncas = 0;
{'\t'}{'\t'}if (auto p = ca->v_constructor_arguments) for (; *p; ++p) ++ncas;
{'\t'}{'\t'}auto RECYCLONE__SPILL cas = {newCATAs}(ncas);
{'\t'}{'\t'}for (size_t i = 0; i < ncas; ++i) {{
{'\t'}{'\t'}{'\t'}auto a = ca->v_constructor_arguments[i];
{'\t'}{'\t'}{'\t'}{constructCATA}(&cas->f_data()[i], a->v_type, value(a));
{'\t'}{'\t'}}}
{'\t'}{'\t'}size_t nnas = 0;
{'\t'}{'\t'}if (auto p = ca->v_named_arguments) for (; *p; ++p) ++nnas;
{'\t'}{'\t'}auto RECYCLONE__SPILL nas = f__new_array<{transpiler.Escape(get(typeof(CustomAttributeNamedArgument[])))}, {transpiler.Escape(typeofCANA)}>(nnas);
{'\t'}{'\t'}for (size_t i = 0; i < nnas; ++i) {{
{'\t'}{'\t'}{'\t'}auto a = ca->v_named_arguments[i];
{'\t'}{'\t'}{'\t'}{transpiler.EscapeForStacked(typeofCATA)} ta{{}};
{'\t'}{'\t'}{'\t'}{constructCATA}(&ta, a->v_type, value(a));
{'\t'}{'\t'}{'\t'}{escape(typeofCANA.GetConstructor([get(typeof(MemberInfo)), typeofCATA]) ?? throw new Exception())}(&nas->f_data()[i], a->v_member, ta);
{'\t'}{'\t'}}}
{'\t'}{'\t'}auto RECYCLONE__SPILL cad = f__new_zerod<{transpiler.Escape(typeofCAD)}>();
{'\t'}{'\t'}{escape(typeofCAD.GetConstructors()[0])}(cad, ca->v_constructor, cas, nas);
{'\t'}{'\t'}p->f_data()[i] = cad;
{'\t'}}}
{'\t'}return p;
", 0);
            }
        );
        code.For(
            type.GetMethod(nameof(RuntimeCustomAttributeData.InternalIsDefined)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__custom_attributes) {{
{'\t'}{'\t'}std::cerr << ""no custom attributes: "";
{'\t'}{'\t'}if (a_0->v__declaring_type) std::cerr << f__string(a_0->v__declaring_type->v__full_name) << ""::"";
{'\t'}{'\t'}std::cerr << '[' << f__string(a_0->f_type() == &t__type_of<t__type>::v__instance ? static_cast<t__type*>(a_0)->v__full_name : a_0->v__name) << ']' << std::endl;
{'\t'}{'\t'}return false;
{'\t'}}}
{'\t'}for (auto p = a_0->v__custom_attributes; *p; ++p) if ((*p)->v_constructor->v__declaring_type->f_is(a_1)) return true;
{'\t'}return false;
", 0)
        );
    })
    .For(get(typeof(RuntimeFieldInfo)), (type, code) =>
    {
        SetupMemberInfo(get, type, code);
        code.For(
            type.GetProperty(nameof(FieldInfo.Attributes))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__attributes;\n", 0)
        );
        code.For(
            type.GetProperty(nameof(FieldInfo.FieldType))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__field_type;\n", 0)
        );
        code.For(
            type.GetMethod(nameof(FieldInfo.GetValue)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (a_1 && !a_1->f_type()->f_is(a_0->v__declaring_type)) [[unlikely]] {transpiler.GenerateThrow("Argument")};
{'\t'}return a_0->v__field_type->f_box(a_0->f_address(a_0->v__declaring_type->f_unbox(const_cast<t__object*&>(a_1))));
", 0)
        );
        code.For(
            type.GetMethod(nameof(FieldInfo.SetValue), [get(typeof(object)), get(typeof(object)), get(typeof(BindingFlags)), get(typeof(Binder)), get(typeof(CultureInfo))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (a_1 && !a_1->f_type()->f_is(a_0->v__declaring_type)) [[unlikely]] {transpiler.GenerateThrow("Argument")};
{'\t'}if (a_2 && !a_2->f_type()->f_assignable_to(a_0->v__field_type)) [[unlikely]] {transpiler.GenerateThrow("Argument")};
{'\t'}a_0->v__field_type->f_copy(a_0->v__field_type->f_unbox(const_cast<t__object*&>(a_2)), 1, a_0->f_address(a_0->v__declaring_type->f_unbox(const_cast<t__object*&>(a_1))));
", 0)
        );
    })
    .For(get(typeof(RuntimeMethodInfo)), (type, code) =>
    {
        SetupMethodBase(get, type, code);
        // TODO: check signature.
        code.For(
            type.GetMethod(nameof(MethodInfo.CreateDelegate), [get(typeof(Type))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__function) {transpiler.GenerateThrow("NotSupported")};
{'\t'}if (a_1->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}auto type = static_cast<t__type*>(a_1);
{'\t'}auto RECYCLONE__SPILL p = type->f_new_zerod();
{'\t'}auto q = static_cast<{transpiler.EscapeForStacked(get(typeof(MulticastDelegate)))}>(p);
{'\t'}q->v__5ftarget = p;
{'\t'}q->v__5fmethodPtr = type->v__invoke_static;
{'\t'}q->v__5fmethodPtrAux = a_0->v__function;
{'\t'}return q;
", 0)
        );
        // TODO: check signature.
        code.For(
            type.GetMethod(nameof(MethodInfo.CreateDelegate), [get(typeof(Type)), get(typeof(object))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__function) {transpiler.GenerateThrow("NotSupported")};
{'\t'}if (a_1->f_type() != &t__type_of<t__type>::v__instance) throw std::runtime_error(""must be t__type"");
{'\t'}auto type = static_cast<t__type*>(a_1);
{'\t'}auto RECYCLONE__SPILL p = type->f_new_zerod();
{'\t'}auto q = static_cast<{transpiler.EscapeForStacked(get(typeof(MulticastDelegate)))}>(p);
{'\t'}if (a_2) {{
{'\t'}{'\t'}q->v__5ftarget = a_2;
{'\t'}{'\t'}q->v__5fmethodPtr = a_0->v__function;
{'\t'}}} else {{
{'\t'}{'\t'}q->v__5ftarget = p;
{'\t'}{'\t'}q->v__5fmethodPtr = type->v__invoke_static;
{'\t'}{'\t'}q->v__5fmethodPtrAux = a_0->v__function;
{'\t'}}}
{'\t'}return q;
", 0)
        );
        code.For(
            type.GetMethod(nameof(MethodBase.Invoke), [get(typeof(object)), get(typeof(BindingFlags)), get(typeof(Binder)), get(typeof(object[])), get(typeof(CultureInfo))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $"\treturn a_0->v__invoke(a_1, a_2, a_3, a_4, a_5);\n", 0)
        );
        code.For(
            type.GetMethod(nameof(MethodInfo.MakeGenericMethod)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__generic_definition || a_0->v__generic_definition != a_0) {transpiler.GenerateThrow("InvalidOperation")};
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__generic_arguments; *p; ++p) ++n;
{'\t'}if (a_1->v__length != n) throw std::runtime_error(""not same number of types: "" + f__string(a_0->v__declaring_type->v__full_name) + ""::"" + f__string(a_0->v__name));
{'\t'}for (auto p = a_0->v__generic_methods; *p; ++p) {{
{'\t'}{'\t'}auto q = (*p)->v__generic_arguments;
{'\t'}{'\t'}if (std::equal(q, q + n, a_1->f_data())) return *p;
{'\t'}}}
{'\t'}auto s = f__string(static_cast<t__type*>(a_1->f_data()[0])->v__full_name);
{'\t'}for (size_t i = 1; i < n; ++i) s += "", "" + f__string(static_cast<t__type*>(a_1->f_data()[i])->v__full_name);
{'\t'}throw std::runtime_error(""not bundled: "" + f__string(a_0->v__declaring_type->v__full_name) + ""::"" + f__string(a_0->v__name) + ""["" + s + ""]"");
", 0)
        );
        code.For(
            type.GetMethod(nameof(RuntimeMethodInfo.GetParentDefinition)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!(a_0->v__attributes & {(int)MethodAttributes.Virtual})) return nullptr;
{'\t'}if (!a_0->v__function) {transpiler.GenerateThrow("InvalidOperation")};
{'\t'}auto type = a_0->v__declaring_type;
{'\t'}size_t i = 0;
{'\t'}while (reinterpret_cast<void**>(type + 1)[i] != a_0->v__function) ++i;
{'\t'}type = type->v__base;
{'\t'}if (!type || type->v__slots <= i) return nullptr;
{'\t'}auto function = reinterpret_cast<void**>(type + 1)[i];
{'\t'}while (auto base = type->v__base) {{
{'\t'}{'\t'}if (base->v__slots <= i || reinterpret_cast<void**>(base + 1)[i] != function) break;
{'\t'}{'\t'}type = base;
{'\t'}}}
{'\t'}auto p = type->v__methods;
{'\t'}if (!p) throw std::runtime_error(""no methods: "" + f__string(type->v__full_name));
{'\t'}while ((*p)->v__function != function) ++p;
{'\t'}return *p;
", 0)
        );
        code.For(
            type.GetProperty(nameof(MethodInfo.ReturnType))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__return_type;\n", 0)
        );
    })
    .For(get(typeof(RuntimePropertyInfo)), (type, code) =>
    {
        SetupMemberInfo(get, type, code);
        code.For(
            type.GetProperty(nameof(PropertyInfo.Attributes))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__attributes;\n", 0)
        );
        code.For(
            type.GetMethod(nameof(PropertyInfo.GetIndexParameters)),
            transpiler => GetParameters(get, transpiler)
        );
        code.For(
            type.GetProperty(nameof(PropertyInfo.GetMethod))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + $"\treturn a_0->v__get;\n", 0)
        );
        code.For(
            type.GetMethod(nameof(PropertyInfo.GetValue), [get(typeof(object)), get(typeof(BindingFlags)), get(typeof(Binder)), get(typeof(object[])), get(typeof(CultureInfo))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__get) [[unlikely]] {transpiler.GenerateThrow("Argument")};
{'\t'}return a_0->v__get->v__invoke(a_1, a_2, a_3, a_4, a_5);
", 0)
        );
        code.For(
            type.GetProperty(nameof(PropertyInfo.SetMethod))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + $"\treturn a_0->v__set;\n", 0)
        );
        code.For(
            type.GetMethod(nameof(PropertyInfo.SetValue), [get(typeof(object)), get(typeof(object)), get(typeof(BindingFlags)), get(typeof(Binder)), get(typeof(object[])), get(typeof(CultureInfo))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__set) [[unlikely]] {transpiler.GenerateThrow("Argument")};
{'\t'}auto n = a_5 ? a_5->v__length : 0;
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(object[])))}, {transpiler.Escape(get(typeof(object)))}>(n + 1);
{'\t'}*std::copy_n(a_5->f_data(), n, p->f_data()) = a_2;
{'\t'}a_0->v__set->v__invoke(a_1, a_3, a_4, p, a_6);
", 0)
        );
        code.For(
            type.GetProperty(nameof(PropertyInfo.PropertyType))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__property_type;\n", 0)
        );
    })
    .For(get(typeof(RuntimeType)), (type, code) =>
    {
        SetupMemberInfo(get, type, code);
        code.For(
            type.GetProperty(nameof(Type.Assembly))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__assembly;\n", 0)
        );
        code.For(
            type.GetProperty(nameof(Type.BaseType))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__base;\n", 0)
        );
        code.For(
            type.GetProperty(nameof(Type.FullName))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__full_name);\n", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetArrayRank)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__array) {transpiler.GenerateThrow("Argument")};
{'\t'}return a_0->v__rank;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetConstructors), [get(typeof(BindingFlags))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__constructors) std::cerr << ""no constructors: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}size_t n = 0;
{'\t'}a_0->f_each_constructor(a_1, [&](auto)
{'\t'}{{
{'\t'}{'\t'}++n;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(ConstructorInfo[])))}, {transpiler.Escape(get(typeof(ConstructorInfo)))}>(n);
{'\t'}auto q = p->f_data();
{'\t'}a_0->f_each_constructor(a_1, [&](auto a_x)
{'\t'}{{
{'\t'}{'\t'}*q++ = a_x;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetElementType)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__array || a_0->v__by_ref || a_0->v__pointer ? a_0->v__element : nullptr;\n", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetEnumName)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__enum) throw std::runtime_error(""not enum"");
{'\t'}if (!a_0->v__fields) throw std::runtime_error(""no fields: "" + f__string(a_0->v__full_name));
{'\t'}if (!a_1->f_type()->f_assignable_to_value(a_0)) [[unlikely]] {transpiler.GenerateThrow("Argument")};
{'\t'}for (auto p = a_0->v__fields; *p; ++p) if (std::memcmp((*p)->f_address(nullptr), a_0->f_unbox(const_cast<t__object*&>(a_1)), a_0->v__size) == 0) return f__new_string((*p)->v__name);
{'\t'}return nullptr;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetEnumNames)),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}if (!a_0->v__enum) throw std::runtime_error(""not enum"");
{'\t'}if (!a_0->v__fields) throw std::runtime_error(""no fields: "" + f__string(a_0->v__full_name));
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__fields; *p; ++p) ++n;
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(string[])))}, {transpiler.Escape(get(typeof(string)))}>(n);
{'\t'}for (size_t i = 0; i < n; ++i) p->f_data()[i] = f__new_string(a_0->v__fields[i]->v__name);
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetEnumValues)),
            transpiler => (transpiler.GenerateCheckArgumentNull("a_0") + $@"{'\t'}if (!a_0->v__enum) throw std::runtime_error(""not enum"");
{'\t'}if (!a_0->v__fields) throw std::runtime_error(""no fields: "" + f__string(a_0->v__full_name));
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__fields; *p; ++p) ++n;
{'\t'}return f__new_array(a_0, n, [&](auto a_p, auto a_n)
{'\t'}{{
{'\t'}{'\t'}for (size_t i = 0; i < n; ++i) {{
{'\t'}{'\t'}{'\t'}std::memcpy(a_p, a_0->v__fields[i]->f_address(nullptr), a_0->v__size);
{'\t'}{'\t'}{'\t'}a_p += a_0->v__size;
{'\t'}{'\t'}}}
{'\t'}}});
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetField), [get(typeof(string)), get(typeof(BindingFlags))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__fields) std::cerr << ""no fields: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}auto name = f__string_view(a_1);
{'\t'}t__runtime_field_info* p = nullptr;
{'\t'}a_0->f_each_field(a_2, [&](auto a_x)
{'\t'}{{
{'\t'}{'\t'}if (a_2 & {(int)BindingFlags.IgnoreCase} ? !std::equal(a_x->v__name.begin(), a_x->v__name.end(), name.begin(), name.end(), [](auto a_x, auto a_y)
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}return std::toupper(a_x) == std::toupper(a_y);
{'\t'}{'\t'}}}) : a_x->v__name != name) return true;
{'\t'}{'\t'}p = a_x;
{'\t'}{'\t'}return false;
{'\t'}}});
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetFields), [get(typeof(BindingFlags))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__fields) std::cerr << ""no fields: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}size_t n = 0;
{'\t'}a_0->f_each_field(a_1, [&](auto)
{'\t'}{{
{'\t'}{'\t'}++n;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(FieldInfo[])))}, {transpiler.Escape(get(typeof(FieldInfo)))}>(n);
{'\t'}auto q = p->f_data();
{'\t'}a_0->f_each_field(a_1, [&](auto a_x)
{'\t'}{{
{'\t'}{'\t'}*q++ = a_x;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetGenericArguments)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__generic_definition) return f__new_array<{transpiler.Escape(get(typeof(Type[])))}, {transpiler.Escape(get(typeof(Type)))}>(0);
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__generic_arguments; *p; ++p) ++n;
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(Type[])))}, {transpiler.Escape(get(typeof(Type)))}>(n);
{'\t'}std::copy_n(a_0->v__generic_arguments, n, p->f_data());
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetGenericTypeDefinition)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__generic_definition) {transpiler.GenerateThrow("InvalidOperation")};
{'\t'}return a_0->v__generic_definition;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetInterfaces)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(Type[])))}, {transpiler.Escape(get(typeof(Type)))}>(a_0->v__interface_to_methods.size());
{'\t'}size_t i = 0;
{'\t'}for (const auto& x : a_0->v__interface_to_methods) p->f_data()[i++] = x.first;
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetMethods), [get(typeof(BindingFlags))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__methods) std::cerr << ""no methods: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}size_t n = 0;
{'\t'}a_0->f_each_method(a_1, [&](auto)
{'\t'}{{
{'\t'}{'\t'}++n;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(MethodInfo[])))}, {transpiler.Escape(get(typeof(MethodInfo)))}>(n);
{'\t'}auto q = p->f_data();
{'\t'}a_0->f_each_method(a_1, [&](auto a_x)
{'\t'}{{
{'\t'}{'\t'}*q++ = a_x;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.GetProperties), [get(typeof(BindingFlags))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $@"{'\t'}if (!a_0->v__properties) std::cerr << ""no properties: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}size_t n = 0;
{'\t'}a_0->f_each_property(a_1, [&](auto)
{'\t'}{{
{'\t'}{'\t'}++n;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}auto RECYCLONE__SPILL p = f__new_array<{transpiler.Escape(get(typeof(PropertyInfo[])))}, {transpiler.Escape(get(typeof(PropertyInfo)))}>(n);
{'\t'}auto q = p->f_data();
{'\t'}a_0->f_each_property(a_1, [&](auto a_x)
{'\t'}{{
{'\t'}{'\t'}*q++ = a_x;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.IsAssignableFrom)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + $"\treturn a_1 && static_cast<t__type*>(a_1)->f_assignable_to(a_0);\n", 0)
        );
        code.For(
            type.GetProperty(nameof(Type.IsByRefLike))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__by_ref_like;\n", 0)
        );
        code.For(
            type.GetProperty(nameof(Type.IsConstructedGenericType))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__generic_definition && a_0->v__generic_definition != a_0;\n", 0)
        );
        code.For(
            type.GetProperty(nameof(Type.IsGenericType))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__generic_definition;\n", 0)
        );
        code.For(
            type.GetProperty(nameof(Type.IsGenericTypeDefinition))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__generic_definition && a_0->v__generic_definition == a_0;\n", 0)
        );
        code.For(
            type.GetMethod(nameof(Type.MakeGenericType)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__generic_definition || a_0->v__generic_definition != a_0) {transpiler.GenerateThrow("InvalidOperation")};
{'\t'}size_t n = 0;
{'\t'}for (auto p = a_0->v__generic_arguments; *p; ++p) ++n;
{'\t'}if (a_1->v__length != n) throw std::runtime_error(""not same number of types: "" + f__string(a_0->v__full_name));
{'\t'}for (auto p = a_0->v__generic_types; *p; ++p) {{
{'\t'}{'\t'}auto q = (*p)->v__generic_arguments;
{'\t'}{'\t'}if (std::equal(q, q + n, a_1->f_data())) return *p;
{'\t'}}}
{'\t'}auto s = f__string(static_cast<t__type*>(a_1->f_data()[0])->v__full_name);
{'\t'}for (size_t i = 1; i < n; ++i) s += "", "" + f__string(static_cast<t__type*>(a_1->f_data()[i])->v__full_name);
{'\t'}throw std::runtime_error(""not bundled: "" + f__string(a_0->v__full_name) + ""["" + s + ""]"");
", 0)
        );
        code.For(
            type.GetProperty(nameof(Type.Namespace))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__namespace);\n", 0)
        );
        code.For(
            type.GetMethod(nameof(object.ToString)),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn f__new_string(a_0->v__display_name);\n", 0)
        );
        code.For(
            type.GetProperty(nameof(Type.TypeHandle))!.GetMethod,
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn {a_0};\n", 0)
        );
        code.For(
            type.GetMethod("GetAttributeFlagsImpl", declaredAndInstance),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__attributes;\n", 0)
        );
        code.For(
            type.GetMethod("GetConstructorImpl", declaredAndInstance),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_4") + $@"{'\t'}if (!a_0->v__constructors) std::cerr << ""no constructors: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}t__runtime_constructor_info* p = nullptr;
{'\t'}a_0->f_each_constructor(a_1, [&](auto a_x)
{'\t'}{{
{'\t'}{'\t'}auto q = a_x->v__parameters;
{'\t'}{'\t'}for (size_t i = 0; i < a_4->v__length; ++i, ++q) if (!*q || (*q)->v__parameter_type != a_4->f_data()[i]) return true;
{'\t'}{'\t'}if (*q) return true;
{'\t'}{'\t'}p = a_x;
{'\t'}{'\t'}return false;
{'\t'}}});
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod("GetMethodImpl", declaredAndInstance, [get(typeof(string)), get(typeof(BindingFlags)), get(typeof(Binder)), get(typeof(CallingConventions)), get(typeof(Type[])), get(typeof(ParameterModifier[]))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__methods) std::cerr << ""no methods: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}auto name = f__string_view(a_1);
{'\t'}t__runtime_method_info* p = nullptr;
{'\t'}a_0->f_each_method(a_2, [&](auto a_x)
{'\t'}{{
{'\t'}{'\t'}if (p && a_x->v__declaring_type != p->v__declaring_type) return false;
{'\t'}{'\t'}if (a_2 & {(int)BindingFlags.IgnoreCase} ? !std::equal(a_x->v__name.begin(), a_x->v__name.end(), name.begin(), name.end(), [](auto a_x, auto a_y)
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}return std::toupper(a_x) == std::toupper(a_y);
{'\t'}{'\t'}}}) : a_x->v__name != name) return true;
{'\t'}{'\t'}if (a_5) {{
{'\t'}{'\t'}{'\t'}auto p = a_x->v__parameters;
{'\t'}{'\t'}{'\t'}for (size_t i = 0; i < a_5->v__length; ++i, ++p) if (!*p || (*p)->v__parameter_type != a_5->f_data()[i]) return true;
{'\t'}{'\t'}{'\t'}if (*p) return true;
{'\t'}{'\t'}}}
{'\t'}{'\t'}if (p) {transpiler.GenerateThrow("AmbiguousMatch")};
{'\t'}{'\t'}p = a_x;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod("GetMethodImpl", declaredAndInstance, [get(typeof(string)), get(typeof(int)), get(typeof(BindingFlags)), get(typeof(Binder)), get(typeof(CallingConventions)), get(typeof(Type[])), get(typeof(ParameterModifier[]))]),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__methods) std::cerr << ""no methods: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}auto name = f__string_view(a_1);
{'\t'}t__runtime_method_info* p = nullptr;
{'\t'}a_0->f_each_method(a_3, [&](auto a_x)
{'\t'}{{
{'\t'}{'\t'}if (p && a_x->v__declaring_type != p->v__declaring_type) return false;
{'\t'}{'\t'}size_t n = 0;
{'\t'}{'\t'}if (a_x->v__generic_definition) for (auto p = a_x->v__generic_arguments; *p; ++p) ++n;
{'\t'}{'\t'}if (n != a_2) return true;
{'\t'}{'\t'}if (a_3 & {(int)BindingFlags.IgnoreCase} ? !std::equal(a_x->v__name.begin(), a_x->v__name.end(), name.begin(), name.end(), [](auto a_x, auto a_y)
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}return std::toupper(a_x) == std::toupper(a_y);
{'\t'}{'\t'}}}) : a_x->v__name != name) return true;
{'\t'}{'\t'}if (a_6) {{
{'\t'}{'\t'}{'\t'}auto p = a_x->v__parameters;
{'\t'}{'\t'}{'\t'}for (size_t i = 0; i < a_6->v__length; ++i, ++p) {{
{'\t'}{'\t'}{'\t'}{'\t'}if (!*p) return true;
{'\t'}{'\t'}{'\t'}{'\t'}auto p0 = (*p)->v__parameter_type;
{'\t'}{'\t'}{'\t'}{'\t'}auto& p1 = a_6->f_data()[i];
{'\t'}{'\t'}{'\t'}{'\t'}if (p0 == p1) continue;
{'\t'}{'\t'}{'\t'}{'\t'}if (p0->f_type() == &t__type_of<t__type>::v__instance) return true;
{'\t'}{'\t'}{'\t'}{'\t'}if (p0->f_type() != p1->f_type()) return true;
{'\t'}{'\t'}{'\t'}{'\t'}if (static_cast<t__generic_parameter*>(p0)->v__position != static_cast<t__generic_parameter*>(p1)->v__position) return true;
{'\t'}{'\t'}{'\t'}}}
{'\t'}{'\t'}{'\t'}if (*p) return true;
{'\t'}{'\t'}}}
{'\t'}{'\t'}if (p) {transpiler.GenerateThrow("AmbiguousMatch")};
{'\t'}{'\t'}p = a_x;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod("GetPropertyImpl", declaredAndInstance),
            transpiler => (transpiler.GenerateCheckNull("a_0") + transpiler.GenerateCheckArgumentNull("a_1") + $@"{'\t'}if (!a_0->v__properties) std::cerr << ""no properties: "" << f__string(a_0->v__full_name) << std::endl;
{'\t'}auto name = f__string_view(a_1);
{'\t'}t__runtime_property_info* p = nullptr;
{'\t'}a_0->f_each_property(a_2, [&](auto a_x)
{'\t'}{{
{'\t'}{'\t'}if (p && a_x->v__declaring_type != p->v__declaring_type) return false;
{'\t'}{'\t'}if (a_2 & {(int)BindingFlags.IgnoreCase} ? !std::equal(a_x->v__name.begin(), a_x->v__name.end(), name.begin(), name.end(), [](auto a_x, auto a_y)
{'\t'}{'\t'}{{
{'\t'}{'\t'}{'\t'}return std::toupper(a_x) == std::toupper(a_y);
{'\t'}{'\t'}}}) : a_x->v__name != name) return true;
{'\t'}{'\t'}if (a_5) {{
{'\t'}{'\t'}{'\t'}auto p = a_x->v__parameters;
{'\t'}{'\t'}{'\t'}for (size_t i = 0; i < a_5->v__length; ++i, ++p) if (!*p || (*p)->v__parameter_type != a_5->f_data()[i]) return true;
{'\t'}{'\t'}{'\t'}if (*p) return true;
{'\t'}{'\t'}}}
{'\t'}{'\t'}if (p) {transpiler.GenerateThrow("AmbiguousMatch")};
{'\t'}{'\t'}p = a_x;
{'\t'}{'\t'}return true;
{'\t'}}});
{'\t'}return p;
", 0)
        );
        code.For(
            type.GetMethod("GetTypeCodeImpl", declaredAndInstance),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__type_code;\n", 0)
        );
        code.For(
            type.GetMethod("HasElementTypeImpl", declaredAndInstance),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__array || a_0->v__by_ref || a_0->v__pointer;\n", 0)
        );
        code.For(
            type.GetMethod("IsArrayImpl", declaredAndInstance),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__array;\n", 0)
        );
        code.For(
            type.GetMethod("IsByRefImpl", declaredAndInstance),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__by_ref;\n", 0)
        );
        code.For(
            type.GetMethod("IsPrimitiveImpl", declaredAndInstance),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->f_primitive();\n", 0)
        );
        code.For(
            type.GetMethod("IsPointerImpl", declaredAndInstance),
            transpiler => (transpiler.GenerateCheckNull("a_0") + "\treturn a_0->v__pointer;\n", 0)
        );
        // TODO
        code.For(
            type.GetMethod(nameof(RuntimeType.ValueEquals)),
            transpiler => ("\treturn a_2 && a_2->f_type() == a_0 && std::memcmp(a_1, a_2 + 1, a_0->v__size) == 0;\n", 0)
        );
        // TODO
        code.For(
            type.GetMethod(nameof(RuntimeType.ValueGetHashCode)),
            transpiler =>
            {
                var marvin = get(Type.GetType("System.Marvin", true)!);
                var seed = marvin.GetProperty("DefaultSeed")!.GetMethod ?? throw new Exception();
                var compute = marvin.GetMethod("ComputeHash32", [get(typeof(byte)).MakeByRefType(), get(typeof(uint)), get(typeof(uint)), get(typeof(uint))]) ?? throw new Exception();
                transpiler.Enqueue(seed);
                transpiler.Enqueue(compute);
                return ($@"{'\t'}auto seed = {transpiler.Escape(seed)}();
{'\t'}return {transpiler.Escape(compute)}(static_cast<uint8_t*>(a_1.v__5fvalue), a_0->v__size, seed, seed >> 32);
", 0);
            }
        );
        // TODO
        code.For(
            type.GetMethod(nameof(RuntimeType.ValueToString)),
            transpiler => ("\treturn f__new_string(a_0->v__display_name);\n", 0)
        );
    })
    .For(get(typeof(RuntimeTimer)), (type, code) =>
    {
        code.For(
            type.GetMethod(nameof(RuntimeTimer.Call)),
            transpiler =>
            {
                var m = get(Type.GetType("System.Threading.TimerQueue", true)!).GetMethod("AppDomainTimerCallback", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new Exception();
                transpiler.Enqueue(m);
                return ($"\t{transpiler.Escape(m)}(a_0);\n", 1);
            }
        );
    });
}
