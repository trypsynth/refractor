#include <prism.h>
#include <stdio.h>
#include <string.h>

int main(int argc, char **argv) {
	const char *expect = argc > 1 ? argv[1] : "ok";
	PrismContext *ctx = prism_init(NULL);
	if (ctx == NULL) return 1;
	PrismBackendId id = prism_registry_id(ctx, "Speech Dispatcher");
	if (id == PRISM_BACKEND_INVALID) {
		puts("Speech Dispatcher is not registered");
		return 1;
	}
	PrismBackend *backend = prism_registry_create(ctx, id);
	printf("available: %s\n", (prism_backend_get_features(backend) & PRISM_BACKEND_IS_SUPPORTED_AT_RUNTIME) ? "yes" : "no");
	PrismError init = prism_backend_initialize(backend);
	printf("initialize: %s\n", prism_error_string(init));
	int result = 0;
	if (strcmp(expect, "missing") == 0) {
		result = init == PRISM_ERROR_BACKEND_NOT_AVAILABLE ? 0 : 1;
	} else {
		PrismError speak = init == PRISM_OK ? prism_backend_speak(backend, "Hello from prism", false) : init;
		printf("speak: %s\n", prism_error_string(speak));
		result = speak == PRISM_OK ? 0 : 1;
	}
	prism_backend_free(backend);
	prism_shutdown(ctx);
	return result;
}
