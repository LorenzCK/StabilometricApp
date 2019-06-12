using System;
using System.Collections.Generic;
using System.Text;

namespace StabilometricApp.Messages {

    public class AlertMessage {

        public AlertMessage(string message) {
            Message = message;
        }

        public string Message { get; private set; }

    }

}
