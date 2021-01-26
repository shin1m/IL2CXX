#include "thread.cc"

int main(int argc, char* argv[])
{
	::t_engine::t_options options;
	options.v_verbose = true;
	::t_engine engine(options);
	engine.f_start_thread([]
	{
		std::this_thread::sleep_for(std::chrono::seconds::max());
	}, true);
	return engine.f_exit(0);
}
