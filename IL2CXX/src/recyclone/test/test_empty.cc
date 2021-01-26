#include <recyclone/engine.h>

using namespace recyclone;

int main(int argc, char* argv[])
{
	t_engine::t_options options;
	options.v_verbose = options.v_verify = true;
	t_engine engine(options);
	return engine.f_exit(0);
}
