package com.brewstract.labs.dsa.scratchpad.problems.shared;

public @interface Complexity {
    String time() default "";

    String space() default "";
}