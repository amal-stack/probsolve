package com.brewstract.labs.dsa.scratchpad.problems.shared;

public @interface Solution {
    String value() default "";

    String[] traits() default {};

    String variantOf() default "";
}