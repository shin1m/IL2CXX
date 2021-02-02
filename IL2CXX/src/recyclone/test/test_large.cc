#include "pair.h"

int main(int argc, char* argv[])
{
	t_engine<t_type>::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine<t_type> engine(options);
	for (size_t i = 0; i < 8; ++i) {
		auto p = static_cast<t_symbol*>(f_engine<t_type>()->f_allocate(sizeof(t_object<t_type>) * (1 << i) + sizeof(std::string)));
		p->f_construct({});
		p->f_bless(&t_type_of<t_symbol>::v_instance);
	}
	return engine.f_exit(0);
}
