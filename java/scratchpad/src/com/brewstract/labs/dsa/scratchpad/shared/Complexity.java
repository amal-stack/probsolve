package com.brewstract.labs.dsa.scratchpad.shared;

public @interface Complexity {
    String time() default "";

    String space() default "";
}