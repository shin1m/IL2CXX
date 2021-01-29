#include "pair.cc"

int main(int argc, char* argv[])
{
	t_engine<t_type>::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine<t_type> engine(options);
	auto p = f_new<t_pair>();
	auto q = f_new<t_pair>(p);
	p->v_tail = q;
	return engine.f_exit(0);
}
