package com.brewstract.labs.dsa.scratchpad.problems.shared;

import java.lang.annotation.Repeatable;

@Repeatable(On.List.class)
public @interface On {
    String platform();

    String id();

    @interface List {
        On[] value();
    }
}