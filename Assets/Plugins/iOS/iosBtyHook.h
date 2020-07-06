//
//  iosBtyHook.h
//  iosBattery
//
//  Created by escuser on 8/5/15.
//  Copyright (c) 2015 escuser. All rights reserved.
//

//#ifndef __iosBattery__iosBtyHook__
//#define __iosBattery__iosBtyHook__

//#include <stdio.h>

//#endif /* defined(__iosBattery__iosBtyHook__) */

#ifdef __cplusplus
extern "C" {
#endif
    void CallMethod();
    const char* BtyState();
    int BtyLevel();
#ifdef __cplusplus
}
#endif

