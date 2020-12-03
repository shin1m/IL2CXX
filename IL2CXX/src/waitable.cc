std::mutex t__waitable::v_shared;
std::condition_variable t__waitable::v_awaken;

bool t__waitable::f_wait_all(t__waitable** a_p, size_t a_n, std::chrono::milliseconds a_timeout)
{
	if (a_n <= 0) throw std::invalid_argument("no waitables");
	auto t = f_after(a_timeout);
	std::unique_lock<std::mutex> lock(v_shared);
	while (true) {
		size_t i = 0;
		for (; i < a_n; ++i) {
			auto p = a_p[i];
			std::unique_lock<std::mutex> lock(p->v_mutex);
			if (p->v_reserved || !p->f_signaled()) break;
			p->v_reserved = true;
		}
		if (i >= a_n) break;
		while (i > 0) {
			auto p = a_p[--i];
			std::unique_lock<std::mutex> lock(p->v_mutex);
			p->v_reserved = false;
		}
		v_awaken.notify_all();
		if (v_awaken.wait_until(lock, t) == std::cv_status::timeout) return false;
	}
	for (size_t i = 0; i < a_n; ++i) {
		auto p = a_p[i];
		std::unique_lock<std::mutex> lock(p->v_mutex);
		p->f_acquire();
		p->v_reserved = false;
	}
	return true;
}

size_t t__waitable::f_wait_any(t__waitable** a_p, size_t a_n, std::chrono::milliseconds a_timeout)
{
	if (a_n <= 0) throw std::invalid_argument("no waitables");
	auto t = f_after(a_timeout);
	std::unique_lock<std::mutex> lock(v_shared);
	do
		for (size_t i = 0; i < a_n; ++i) {
			auto p = a_p[i];
			std::unique_lock<std::mutex> lock(p->v_mutex);
			if (p->v_reserved || !p->f_signaled()) continue;
			p->f_acquire();
			return i;
		}
	while (v_awaken.wait_until(lock, t) != std::cv_status::timeout);
	return a_n;
}

bool t__waitable::f_wait(std::chrono::milliseconds a_timeout)
{
	std::unique_lock<std::mutex> lock(v_mutex);
	return v_signal.wait_until(lock, f_after(a_timeout), [&]
	{
		if (v_reserved || !f_signaled()) return false;
		f_acquire();
		return true;
	});
}

bool t__mutex::f_signaled()
{
	return v_count <= 0;
}

void t__mutex::f_acquire()
{
	v_owner = std::this_thread::get_id();
	++v_count;
}

void t__mutex::f_signal()
{
	f_release();
}

void t__mutex::f_release()
{
	std::unique_lock<std::mutex> lock(v_mutex);
	if (std::this_thread::get_id() != v_owner) throw std::domain_error("not owned");
	if (--v_count > 0) return;
	v_owner = {};
	v_signal.notify_all();
	v_awaken.notify_all();
}

bool t__event::f_signaled()
{
	return v_signaled;
}

void t__event::f_acquire()
{
	if (!v_manual) v_signaled = false;
}

void t__event::f_signal()
{
	f_set();
}

void t__event::f_set()
{
	std::lock_guard<std::mutex> lock(v_mutex);
	v_signaled = true;
	v_signal.notify_all();
	v_awaken.notify_all();
}

void t__event::f_reset()
{
	std::lock_guard<std::mutex> lock(v_mutex);
	v_signaled = false;
}

bool t__semaphore::f_signaled()
{
	return v_count > 0;
}

void t__semaphore::f_acquire()
{
	--v_count;
}

void t__semaphore::f_signal()
{
	f_release(1);
}

size_t t__semaphore::f_release(size_t a_count)
{
	std::lock_guard<std::mutex> lock(v_mutex);
	size_t n = v_count;
	if (n + a_count > v_maximum) throw std::invalid_argument("count exceeds maximum");
	v_count += a_count;
	v_signal.notify_all();
	v_awaken.notify_all();
	return n;
}
