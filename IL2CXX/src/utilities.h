template<typename T, typename... T_an>
T* f__new_constructed(T_an&&... a_n)
{
	t__new<T> p(0);
	p->f_construct(std::forward<T_an>(a_n)...);
	return p;
}

template<typename T_array, typename T_element>
T_array* f__new_array(size_t a_length)
{
	t__new<T_array> p(sizeof(T_element) * a_length);
	p->v__length = a_length;
	p->v__bounds[0] = {a_length, 0};
	std::memset(p->f_data(), 0, sizeof(T_element) * a_length);
	return p;
}

std::u16string f__u16string(std::string_view a_x);
std::string f__string(std::u16string_view a_x);

RECYCLONE__ALWAYS_INLINE inline t_System_2eString* f__new_string(size_t a_length)
{
	t__new<t_System_2eString> p(sizeof(char16_t) * a_length);
	p->v__5fstringLength = a_length;
	(&p->v__5ffirstChar)[a_length] = u'\0';
	return p;
}
t_System_2eString* f__new_string(std::string_view a_x);
inline t_System_2eString* f__new_string(std::u16string_view a_value)
{
	auto p = f__new_string(a_value.size());
	std::copy_n(a_value.data(), a_value.size(), &p->v__5ffirstChar);
	return p;
}
inline t_System_2eString* f__new_string(const char* a_x)
{
	return f__new_string(std::string_view(a_x));
}
inline t_System_2eString* f__new_string(const char16_t* a_x)
{
	return f__new_string(std::u16string_view(a_x));
}
char* f__copy_to(const t_System_2eString* a_x, char* a_p, char* a_q);

template<typename T, typename = void>
struct t_has_destroy : std::false_type {};
template<typename T>
struct t_has_destroy<T, std::void_t<decltype(std::declval<T>().f_destroy())>> : std::true_type {};

template<typename T>
inline void f__marshal_in(T& a_x, const T& a_y)
{
	a_x = a_y;
}
template<typename T>
inline void f__marshal_out(const T& a_x, T& a_y)
{
	a_y = a_x;
}
template<typename T>
inline std::enable_if_t<std::negation_v<t_has_destroy<T>>> f__marshal_destroy(T)
{
}

inline void f__marshal_in(int32_t& a_x, const bool& a_y)
{
	a_x = a_y ? 1 : 0;
}
inline void f__marshal_out(const int32_t& a_x, bool& a_y)
{
	a_y = static_cast<bool>(a_x);
}

template<typename T, typename U>
inline auto f__marshal_in(T& a_x, const U& a_y) -> decltype(a_x.f_in(&a_y))
{
	a_x.f_in(&a_y);
}
template<typename T, typename U>
inline auto f__marshal_out(const T& a_x, U& a_y) -> decltype(a_x.f_out(&a_y))
{
	a_x.f_out(&a_y);
}
template<typename T>
inline auto f__marshal_destroy(T& a_x) -> decltype(a_x.f_destroy())
{
	a_x.f_destroy();
}

template<typename T, typename U>
inline auto f__marshal_in(T& a_x, const t_slot_of<U>& a_y) -> decltype(a_x.f_in(a_y))
{
	a_x.f_in(a_y);
}
template<typename T, typename U>
inline auto f__marshal_out(const T& a_x, t_slot_of<U>& a_y) -> decltype(a_x.f_out(a_y))
{
	auto RECYCLONE__SPILL p = f__new_zerod<U>();
	a_x.f_out(p);
	f__store(a_y, p);
}

inline void f__marshal_in(char*& a_x, const t_slot_of<t_System_2eString>& a_y)
{
	auto n = a_y->v__5fstringLength * MB_LEN_MAX + 1;
	a_x = new char[n];
	f__copy_to(a_y, a_x, a_x + n);
}
inline void f__marshal_in(char16_t*& a_x, const t_slot_of<t_System_2eString>& a_y)
{
	auto n = a_y->v__5fstringLength + 1;
	a_x = new char16_t[n];
	std::copy_n(&a_y->v__5ffirstChar, n, a_x);
}
template<size_t N>
inline void f__marshal_in(char (&a_x)[N], const t_slot_of<t_System_2eString>& a_y)
{
	f__copy_to(a_y, a_x, a_x + N);
}
template<size_t N>
inline void f__marshal_in(char16_t (&a_x)[N], const t_slot_of<t_System_2eString>& a_y)
{
	*std::copy_n(&a_y->v__5ffirstChar, std::min(static_cast<size_t>(a_y->v__5fstringLength), N - 1), a_x) = u'\0';
}
template<typename T>
inline void f__marshal_out(const T* a_x, t_slot_of<t_System_2eString>& a_y)
{
	f__store(a_y, f__new_string(a_x));
}
inline void f__marshal_destroy(void*)
{
}
template<typename T>
inline void f__marshal_destroy(T*& a_x)
{
	delete a_x;
}
template<typename T, size_t N>
inline void f__marshal_destroy(T (&)[N])
{
}

std::vector<char16_t> f__to_cs16(t_System_2eText_2eStringBuilder* a_p);
void f__from(t_System_2eText_2eStringBuilder* a_p, const char16_t* a_cs);
std::vector<char> f__to_cs(t_System_2eText_2eStringBuilder* a_p);
void f__from(t_System_2eText_2eStringBuilder* a_p, const char* a_cs);

t__type* f__find_type(const std::map<std::string_view, t__type*>& a_name_to_type, std::u16string_view a_name);

extern RECYCLONE__THREAD int32_t v_last_unmanaged_error;

inline std::u16string_view f__string_view(t_System_2eString* a_p)
{
	return {&a_p->v__5ffirstChar, static_cast<size_t>(a_p->v__5fstringLength)};
}
inline std::string f__string(t_System_2eString* a_p)
{
	return f__string(f__string_view(a_p));
}
t_System_2eString* f__to_string(t__object* a_p);

template<typename T>
t_System_2eArray* f__new_array(t__type* a_type, size_t a_length, T a_do)
{
	auto a = sizeof(t_System_2eArray) + sizeof(t_System_2eArray::t__bound);
	auto n = a_type->v__size * a_length;
	auto p = static_cast<t_System_2eArray*>(f_engine()->f_allocate(a + n));
	p->v__length = a_length;
	p->f_bounds()[0] = {a_length, 0};
	a_do(reinterpret_cast<char*>(p) + a, n);
	a_type->v__szarray->f_finish(p);
	return p;
}
