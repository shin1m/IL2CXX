#include "pair.cc"

int main(int argc, char* argv[])
{
	t_engine::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine engine(options);
	auto p = f_cons();
	auto q = f_cons(p);
	p->v_tail = q;
	return engine.f_exit(0);
}
