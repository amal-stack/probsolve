package com.brewstract.labs.dsa.scratchpad.problems.shared;

public @interface Problem {

    Status status() default Status.PENDING;

    public static enum Status {
        SOLVED, PENDING
    }
}