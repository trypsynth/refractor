#include <prism.h>
#include <stdio.h>
#include <string.h>

int main(int argc, char **argv) {
	if (argc < 3) return 2;
	PrismContext *ctx = prism_init(NULL);
	if (ctx == NULL) return 1;
	PrismBackendId id = prism_registry_id(ctx, argv[1]);
	if (id == PRISM_BACKEND_INVALID) {
		printf("%s is not registered\n", argv[1]);
		return 1;
	}
	PrismBackend *backend = prism_registry_create(ctx, id);
	printf("available: %s\n", (prism_backend_get_features(backend) & PRISM_BACKEND_IS_SUPPORTED_AT_RUNTIME) ? "yes" : "no");
	PrismError init = prism_backend_initialize(backend);
	printf("initialize: %s\n", prism_error_string(init));
	prism_backend_free(backend);
	prism_shutdown(ctx);
	if (strcmp(argv[2], "missing") == 0) return init == PRISM_ERROR_BACKEND_NOT_AVAILABLE ? 0 : 1;
	return 0;
}
