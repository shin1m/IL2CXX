#include <recyclone/engine.h>

using namespace std::literals;
using namespace recyclone;

struct t_pair : t_object
{
	t_slot_of<t_object> v_head;
	t_slot_of<t_object> v_tail;

	struct t_type : recyclone::t_type
	{
		static t_type v_instance;

		static void f_do_scan(t_object* a_this, t_scan a_scan)
		{
			auto p = static_cast<t_pair*>(a_this);
			a_scan(p->v_head);
			a_scan(p->v_tail);
		}
		t_type()
		{
			f_scan = f_do_scan;
		}
	};
};
t_pair::t_type t_pair::t_type::v_instance;

t_pair* f_cons(t_object* a_head = nullptr, t_object* a_tail = nullptr)
{
	auto p = static_cast<t_pair*>(f_engine()->f_object__allocate(sizeof(t_pair)));
	new(&p->v_head) decltype(p->v_head)(a_head);
	new(&p->v_tail) decltype(p->v_tail)(a_tail);
	t_pair::t_type::v_instance.f_finish(p);
	return p;
}

struct t_symbol : t_object
{
	std::string v_name;

	struct t_type : recyclone::t_type
	{
		static t_type v_instance;
	};
};
t_symbol::t_type t_symbol::t_type::v_instance;

t_symbol* f_symbol(std::string_view a_name)
{
	auto p = static_cast<t_symbol*>(f_engine()->f_object__allocate(sizeof(t_symbol)));
	new(&p->v_name) decltype(p->v_name)(a_name);
	t_symbol::t_type::v_instance.f_finish(p);
	return p;
}

std::string f_string(t_object* a_value)
{
	if (!a_value) return "()";
	if (a_value->f_type() == &t_pair::t_type::v_instance)
		for (auto s = "("s;; s += ' ') {
			auto p = static_cast<t_pair*>(a_value);
			s += f_string(p->v_head);
			a_value = p->v_tail;
			if (!a_value) return s + ')';
			if (a_value->f_type() != &t_pair::t_type::v_instance) return s + " . " + f_string(a_value) + ')';
		}
	if (a_value->f_type() == &t_symbol::t_type::v_instance) return static_cast<t_symbol*>(a_value)->v_name;
	throw std::runtime_error("unknown type");
}
