#include <prism.h>
#include <stdio.h>
#include <unistd.h>

static void on_change(void *userdata, PrismBackendId backend, const char *name, bool available) {
	(void)userdata;
	(void)backend;
	printf("availability: %s %s\n", name, available ? "up" : "down");
}

int main(void) {
	printf("auto power management: %s\n", prism_availability_auto_power_supported() ? "yes" : "no");
	PrismConfig cfg = prism_config_init();
	cfg.availability_callback = on_change;
	cfg.availability_auto_power_manage = true;
	PrismContext *ctx = prism_init(&cfg);
	if (ctx == NULL) return 1;
	PrismBackendId id = prism_registry_id(ctx, "Orca");
	if (id == PRISM_BACKEND_INVALID) {
		puts("Orca is not registered");
		return 1;
	}
	PrismBackend *backend = prism_registry_create(ctx, id);
	PrismError init = prism_backend_initialize(backend);
	printf("initialize without Orca running: %s\n", prism_error_string(init));
	sleep(2);
	prism_backend_free(backend);
	prism_shutdown(ctx);
	return init == PRISM_ERROR_BACKEND_NOT_AVAILABLE ? 0 : 1;
}
