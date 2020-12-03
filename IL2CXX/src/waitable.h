class t__waitable
{
	static std::chrono::time_point<std::chrono::steady_clock> f_after(std::chrono::milliseconds a_timeout)
	{
		return a_timeout == std::chrono::milliseconds::max() ? std::chrono::time_point<std::chrono::steady_clock>::max() : std::chrono::steady_clock::now() + a_timeout;
	}

	static std::mutex v_shared;

protected:
	static std::condition_variable v_awaken;

	std::mutex v_mutex;
	std::condition_variable v_signal;
	bool v_reserved = false;

	virtual bool f_signaled() = 0;
	virtual void f_acquire() = 0;

public:
	static bool f_wait_all(t__waitable** a_p, size_t a_n, std::chrono::milliseconds a_timeout);
	static size_t f_wait_any(t__waitable** a_p, size_t a_n, std::chrono::milliseconds a_timeout);

	virtual ~t__waitable() = default;
	bool f_wait(std::chrono::milliseconds a_timeout);
	virtual void f_signal() = 0;
};

class t__mutex : public t__waitable
{
	std::thread::id v_owner;
	size_t v_count = 0;

protected:
	virtual bool f_signaled();
	virtual void f_acquire();

public:
	t__mutex(bool a_signaled)
	{
		if (a_signaled) f_acquire();
	}
	virtual void f_signal();
	void f_release();
};

class t__event : public t__waitable
{
	bool v_manual;
	bool v_signaled;

protected:
	virtual bool f_signaled();
	virtual void f_acquire();

public:
	t__event(bool a_manual, bool a_signaled) : v_manual(a_manual), v_signaled(a_signaled)
	{
	}
	virtual void f_signal();
	void f_set();
	void f_reset();
};

class t__semaphore : public t__waitable
{
	size_t v_maximum;
	size_t v_count;

protected:
	virtual bool f_signaled();
	virtual void f_acquire();

public:
	t__semaphore(size_t a_maximum, size_t a_count) : v_maximum(a_maximum), v_count(a_count)
	{
	}
	virtual void f_signal();
	size_t f_release(size_t a_count);
};
