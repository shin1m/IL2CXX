#include <recyclone/engine.h>

using namespace recyclone;

//! An example type descriptor that is also an object.
struct t_type : t_object<t_type>
{
	//! Called by the collector to scan object members.
	void (*f_scan)(t_object<t_type>*, t_scan<t_type>);
};

template<typename T>
struct t_type_of : t_type
{
	template<typename... T_an>
	T* f_new(T_an&&... a_n)
	{
		auto p = static_cast<T*>(f_engine<t_type>()->f_allocate(sizeof(T)));
		p->f_construct(std::forward<T_an>(a_n)...);
		// Finishes object construction.
		p->f_bless(this);
		return p;
	}
};

template<>
struct t_type_of<t_type> : t_type
{
	//! Constructs t_type_of<t_type>.
	static t_type_of* f_initialize()
	{
		auto p = static_cast<t_type_of*>(f_engine<t_type>()->f_allocate(sizeof(t_type_of)));
		p->f_scan = [](auto, auto)
		{
		};
		// t_type_of<t_type> is also an instance of t_type_of<t_type>.
		p->f_bless(p);
		return p;
	}

	//! Constructs t_type_of<T>.
	template<typename T>
	t_type_of<T>* f_new()
	{
		auto p = static_cast<t_type_of<T>*>(f_engine<t_type>()->f_allocate(sizeof(t_type_of<T>)));
		p->f_scan = [](auto a_this, auto a_scan)
		{
			// Just delegates to a_this.
			static_cast<T*>(a_this)->f_scan(a_scan);
		};
		// t_type_of<T> is an instance of t_type_of<t_type>.
		p->f_bless(this);
		return p;
	}
};

struct t_pair : t_object<t_type>
{
	t_slot_of<t_object<t_type>> v_head;
	t_slot_of<t_object<t_type>> v_tail;

	//! Called by t_type_of<t_pair>::f_new(...).
	void f_construct(t_object<t_type>* a_head = nullptr, t_object<t_type>* a_tail = nullptr)
	{
		// Members have not been initialized yet at this point.
		new(&v_head) decltype(v_head)(a_head);
		new(&v_tail) decltype(v_tail)(a_tail);
	}
	//! Called by t_typeof<t_pair>::f_scan(...).
	void f_scan(t_scan<t_type> a_scan)
	{
		a_scan(v_head);
		a_scan(v_tail);
	}
};

int main(int argc, char* argv[])
{
	t_engine<t_type>::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine<t_type> engine(options);
	auto type_type = t_type_of<t_type>::f_initialize();
	assert(type_type->f_type() == type_type);
	auto type_pair = type_type->f_new<t_pair>();
	assert(type_pair->f_type() == type_type);
	assert(type_pair->f_new(type_pair->f_new(), type_pair->f_new())->f_type() == type_pair);
	return engine.f_exit(0);
}
