#include <recyclone/engine.h>

using namespace std::literals;
using namespace recyclone;

struct t_type
{
	void (*f_scan)(t_object<t_type>*, t_scan<t_type>);
	template<void (t_object<t_type>::*A_push)()>
	void f_push()
	{
	}
	void f_decrement_push()
	{
	}
	void f_cyclic_decrement_push()
	{
	}
	void f_own()
	{
	}
};

template<typename T>
struct t_type_of : t_type
{
	static inline t_type_of v_instance;

	t_type_of()
	{
		f_scan = [](auto a_this, auto a_scan)
		{
			static_cast<T*>(a_this)->f_scan(a_scan);
		};
	}
};

template<typename T, typename... T_an>
T* f_new(T_an&&... a_n)
{
	auto p = static_cast<T*>(f_engine<t_type>()->f_object__allocate(sizeof(T)));
	p->f_construct(std::forward<T_an>(a_n)...);
	p->f_bless(&t_type_of<T>::v_instance);
	return p;
}

struct t_pair : t_object<t_type>
{
	t_slot_of<t_object<t_type>> v_head;
	t_slot_of<t_object<t_type>> v_tail;

	void f_construct(t_object<t_type>* a_head = nullptr, t_object<t_type>* a_tail = nullptr)
	{
		new(&v_head) decltype(v_head)(a_head);
		new(&v_tail) decltype(v_tail)(a_tail);
	}
	void f_scan(t_scan<t_type> a_scan)
	{
		a_scan(v_head);
		a_scan(v_tail);
	}
};

struct t_symbol : t_object<t_type>
{
	std::string v_name;

	void f_construct(std::string_view a_name)
	{
		new(&v_name) decltype(v_name)(a_name);
	}
	void f_scan(t_scan<t_type>)
	{
	}
};

std::string f_string(t_object<t_type>* a_value)
{
	if (!a_value) return "()";
	if (a_value->f_type() == &t_type_of<t_pair>::v_instance)
		for (auto s = "("s;; s += ' ') {
			auto p = static_cast<t_pair*>(a_value);
			s += f_string(p->v_head);
			a_value = p->v_tail;
			if (!a_value) return s + ')';
			if (a_value->f_type() != &t_type_of<t_pair>::v_instance) return s + " . " + f_string(a_value) + ')';
		}
	if (a_value->f_type() == &t_type_of<t_symbol>::v_instance) return static_cast<t_symbol*>(a_value)->v_name;
	throw std::runtime_error("unknown type");
}
