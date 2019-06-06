using System;
using System.Collections.Generic;
using System.Text;

namespace StabilometricApp {

    public struct Reading {

        public long Ticks;

        public double AccX, AccY, AccZ;

        public double GravX, GravY, GravZ;

        public double GyroX, GyroY, GyroZ;

    }

}
