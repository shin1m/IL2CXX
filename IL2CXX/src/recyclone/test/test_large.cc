#include "pair.cc"

int main(int argc, char* argv[])
{
	t_engine::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine engine(options);
	for (size_t i = 0; i < 8; ++i) {
		auto p = static_cast<t_symbol*>(f_engine()->f_object__allocate(sizeof(t_object) * (1 << i) + sizeof(std::string)));
		new(&p->v_name) decltype(p->v_name)();
		t_symbol::t_type::v_instance.f_finish(p);
	}
	return engine.f_exit(0);
}
