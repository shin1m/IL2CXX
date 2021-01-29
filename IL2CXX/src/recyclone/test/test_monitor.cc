#include <functional>
#include "thread.cc"

int main(int argc, char* argv[])
{
	::t_engine::t_options options;
	options.v_verbose = options.v_verify = true;
	::t_engine engine(options);
	auto monitor = f_new<t_symbol>("monitor"sv);
	auto& mutex = monitor->f_extension()->v_mutex;
	auto& condition = monitor->f_extension()->v_condition;
	std::function<void()> action = []
	{
	};
	::t_thread* worker;
	{
		std::unique_lock lock(mutex);
		worker = engine.f_start_thread([&]
		{
			std::printf("start\n");
			try {
				while (true) {
					{
						std::unique_lock lock(mutex);
						action = nullptr;
						condition.notify_one();
						condition.wait(lock, [&]
						{
							return action;
						});
					}
					action();
				}
			} catch (std::nullptr_t) { }
			std::printf("exit\n");
		});
		condition.wait(lock, [&]
		{
			return !action;
		});
	}
	auto send = [&](auto x)
	{
		std::unique_lock lock(mutex);
		action = x;
		condition.notify_one();
		condition.wait(lock, [&]
		{
			return !action;
		});
	};
	auto log = ""s;
	send([&]
	{
		log += "Hello, ";
	});
	send([&]
	{
		log += "World.";
	});
	{
		std::unique_lock lock(mutex);
		action = []
		{
			throw nullptr;
		};
		condition.notify_one();
	}
	engine.f_join(worker);
	std::printf("%s\n", log.c_str());
	assert(log == "Hello, World.");
	return engine.f_exit(0);
}
