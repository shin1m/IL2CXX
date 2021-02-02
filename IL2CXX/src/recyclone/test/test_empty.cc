#include "type.h"

int main(int argc, char* argv[])
{
	t_engine<t_type>::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine<t_type> engine(options);
	return engine.f_exit(0);
}
