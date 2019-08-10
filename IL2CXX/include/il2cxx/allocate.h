template<typename T, typename... T_an>
T* f__new(T_an&&... a_n)
{
	auto p = new T(std::forward<T_an>(a_n)...);
	p->v__type = &t__type_of<T>::v__instance;
	return p;
}

template<typename T, typename... T_an>
T* f__new_sized(size_t a_extra, T_an&&... a_n)
{
	auto p = new(new char[sizeof(T) + a_extra]) T(std::forward<T_an>(a_n)...);
	p->v__type = &t__type_of<T>::v__instance;
	return p;
}

template<typename T>
T* f__new_zerod()
{
	auto p = f__new<T>();
	std::fill_n(reinterpret_cast<char*>(p) + sizeof(t__type*), sizeof(T) - sizeof(t__type*), '\0');
	return p;
}

template<typename T_array, typename T_element>
T_array* f__new_array(size_t a_length)
{
	auto p = f__new_sized<T_array>(sizeof(T_element) * a_length);
	p->v__length = a_length;
	p->v__bounds[0] = {a_length, 0};
	std::fill_n(p->f__data(), a_length, T_element{});
	return p;
}

t_System_2eString* f__new_string(size_t a_length)
{
	auto p = f__new_sized<t_System_2eString>(sizeof(char16_t) * a_length);
	p->v__length = a_length;
	return p;
}

t_System_2eString* f__string(std::u16string_view a_value)
{
	auto p = f__new_string(a_value.size());
	std::copy(a_value.begin(), a_value.end(), reinterpret_cast<char16_t*>(p + 1));
	return p;
}
