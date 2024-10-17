idf_build_get_property(target IDF_TARGET)

idf_component_register(
    SRCS "main.c"
    INCLUDE_DIRS "."
    REQUIRES bg_net 
)