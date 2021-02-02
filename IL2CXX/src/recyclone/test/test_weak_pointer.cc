#include "thread.h"
#include "pair.h"

t_root<t_slot_of<t_symbol>> v_resurrected;

int main(int argc, char* argv[])
{
	t_engine<t_type>::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine_with_finalizer engine(options, [](auto a_p)
	{
		if (a_p->f_type() != &t_type_of<t_symbol>::v_instance) return;
		auto p = static_cast<t_symbol*>(a_p);
		if (static_cast<t_symbol*>(p)->v_name == "resurrected"sv) return;
		p->v_name = "resurrected"sv;
		v_resurrected = p;
		p->f_finalizee__(true);
	});
	std::unique_ptr<t_weak_pointer<t_type>> w;
	f_padding([&]
	{
		auto x = f_new<t_symbol>("foo"sv);
		w = std::make_unique<t_weak_pointer<t_type>>(x, false);
		assert(w->f_target() == x);
	});
	engine.f_collect();
	assert(w->f_target() == nullptr);
	f_padding([&]
	{
		auto x = f_new<t_symbol>("bar"sv);
		x->f_finalizee__(true);
		w = std::make_unique<t_weak_pointer<t_type>>(x, true);
		assert(w->f_target() == x);
	});
	engine.f_collect();
	engine.f_finalize();
	f_padding([&]
	{
		assert(w->f_target() != nullptr);
	});
	v_resurrected = nullptr;
	engine.f_collect();
	engine.f_finalize();
	engine.f_collect();
	assert(w->f_target() == nullptr);
	f_padding([&]
	{
		auto x = f_new<t_symbol>("foo"sv);
		w = std::make_unique<t_weak_pointer<t_type>>(x, true);
		auto y = f_new<t_symbol>("bar"sv);
		w->f_target__(y);
		assert(w->f_target() == y);
	});
	return engine.f_exit(0);
}
