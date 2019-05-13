using System;
using System.Collections.Generic;
using System.Text;

namespace StabilometricApp.Models {
    public class SimpleMessage {
        public enum MessageType {
            START,
            STOP
        }

        public MessageType Type { private set; get; }

        public SimpleMessage(MessageType type ) {
            Type = type;
        }

    }
}
