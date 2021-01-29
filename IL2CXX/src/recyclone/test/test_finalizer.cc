#include "thread.cc"

size_t v_finalized;
t_root<t_slot_of<t_pair>> v_resurrected;

int main(int argc, char* argv[])
{
	::t_engine::t_options options;
	options.v_verbose = options.v_verify = true;
	::t_engine engine(options);
	engine.f_start_finalizer([](auto a_p)
	{
		if (a_p->f_type() != &t_type_of<t_pair>::v_instance) return;
		std::printf("%s -> ", f_string(a_p).c_str());
		auto p = static_cast<t_pair*>(a_p);
		if (p->v_head && p->v_head->f_type() == &t_type_of<t_symbol>::v_instance && static_cast<t_symbol*>(p->v_head)->v_name == "resurrected"sv) {
			++v_finalized;
			std::printf("finalized\n");
		} else {
			p->v_head = f_new<t_symbol>("resurrected"sv);
			v_resurrected = p;
			p->f_finalizee__(true);
			std::printf("resurrected\n");
		}
	});
	f_padding([]
	{
		auto p = f_new<t_pair>(f_new<t_symbol>("foo"sv));
		p->f_finalizee__(true);
	});
	engine.f_collect();
	engine.f_finalize();
	f_padding([]
	{
		std::printf("resurrected: %s\n", f_string(v_resurrected).c_str());
		assert(v_resurrected);
		v_resurrected = nullptr;
	});
	engine.f_collect();
	engine.f_finalize();
	std::printf("finalized: %d\n", v_finalized);
	assert(v_finalized == 1);
	return engine.f_exit(0);
}
