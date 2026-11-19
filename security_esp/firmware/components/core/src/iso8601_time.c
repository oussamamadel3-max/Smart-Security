#include "iso8601_time.h"
#include <stdio.h>
#include <string.h>

/* Howard Hinnant's days-from-civil, public domain:
 * http://howardhinnant.github.io/date_algorithms.html#days_from_civil
 * Returns days since 1970-01-01 for the proleptic Gregorian calendar. */
static int64_t days_from_civil(int64_t y, unsigned m, unsigned d)
{
    y -= (m <= 2) ? 1 : 0;
    int64_t era = (y >= 0 ? y : y - 399) / 400;
    unsigned yoe = (unsigned)(y - era * 400);
    unsigned doy = (153 * (m + (m > 2 ? (unsigned)-3 : 9)) + 2) / 5 + d - 1;
    unsigned doe = yoe * 365 + yoe / 4 - yoe / 100 + doy;
    return era * 146097 + (int64_t)doe - 719468;
}

bool iso8601_utc_to_epoch_ms(const char *text, int64_t *out_epoch_ms)
{
    if (text == NULL || strlen(text) != 20) {
        return false;
    }
    int year, month, day, hour, minute, second;
    int consumed = 0;
    int matched = sscanf(text, "%4d-%2d-%2dT%2d:%2d:%2dZ%n",
                         &year, &month, &day, &hour, &minute, &second, &consumed);
    if (matched != 6 || consumed != 20) {
        return false;
    }
    if (month < 1 || month > 12 || day < 1 || day > 31 ||
        hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59) {
        return false;
    }

    int64_t days = days_from_civil(year, (unsigned)month, (unsigned)day);
    *out_epoch_ms = days * 86400000LL
                  + (int64_t)hour * 3600000LL
                  + (int64_t)minute * 60000LL
                  + (int64_t)second * 1000LL;
    return true;
}
