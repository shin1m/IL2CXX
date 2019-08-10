struct t__type : t_System_2eType
{
	t__type* v__base;
	std::map<t_System_2eType*, void**> v__interface_to_methods;
	size_t v__size;
	t__type* v__element;
	size_t v__rank;

	t__type(t__type* a_base, std::map<t_System_2eType*, void**>&& a_interface_to_methods, size_t a_size, t__type* a_element = nullptr, size_t a_rank = 0) : v__base(a_base), v__interface_to_methods(std::move(a_interface_to_methods)), v__size(a_size), v__element(a_element), v__rank(a_rank)
	{
	}
	bool f__is(t__type* a_type) const
	{
		auto p = this;
		do {
			if (p == a_type) return true;
			p = p->v__base;
		} while (p);
		return false;
	}
	void** f__implementation(t_System_2eType* a_interface) const
	{
		auto i = v__interface_to_methods.find(a_interface);
		return i == v__interface_to_methods.end() ? nullptr : i->second;
	}
};

template<typename T>
struct t__type_of;
