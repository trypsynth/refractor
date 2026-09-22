#include <prism.h>
#include <stdio.h>

int main(void) {
	PrismConfig cfg = prism_config_init();
	PrismContext *ctx = prism_init(&cfg);
	if (ctx == NULL) {
		puts("prism_init failed");
		return 1;
	}
	size_t n = prism_registry_count(ctx);
	printf("registered backends: %zu\n", n);
	for (size_t i = 0; i < n; i++) printf("  %s\n", prism_registry_name(ctx, prism_registry_id_at(ctx, i)));
	prism_shutdown(ctx);
	return 0;
}
