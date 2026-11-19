#include "hw_rng.h"
#include "esp_random.h"

void hw_rng_fill(void *ctx, uint8_t *out, size_t len)
{
    (void)ctx;
    esp_fill_random(out, len);
}
