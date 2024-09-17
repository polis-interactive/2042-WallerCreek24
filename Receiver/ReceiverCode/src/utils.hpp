#ifndef POLIS_UTILS_HPP
#define POLIS_UTILS_HPP

#include <Arduino.h>

#include <string>


void infinitePrint(const std::string &to_log) {
    while (1) {
        Serial.println(to_log.c_str());
        delay(3000);
    }
}

bool beginsWith(const char *str, const std::string &prefix) {
    return strncmp(str, prefix.c_str(), prefix.size()) == 0;
}

bool endsWith(const char* str, const std::string &suffix) {
  const auto strLen = strlen(str);
  const auto suffixLen = suffix.size();
  if (suffixLen > strLen) {
    return false;
  }
  return strcmp(str + strLen - suffixLen, suffix.c_str()) == 0;
}

bool isIncreasing(uint8_t previous, uint8_t current) {
    return (current - previous) < 128;
}

#endif // POLIS_UTILS_HPP