using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoggerLibrary
{
    public class LogHelper
    {

        public enum PredefinedLogMessages
        { 
            USER_INPUT_ACCEPTED = 1,
            
            //File Not found; File in not proper format; File- Access Denied

            PROCESSING_AUTHORING_FILES_PASSED = 2,
            PROCESSING_AUTHORING_FILES_FAILED = 3, 
            
            IDENTIFICATION_TEST_MATRIX_PASSED = 4,
            IDENTIFICATION_TEST_MATRIX_FAILED = 5,
            
            CREATION_CONTEXT_BLOCK_PASSED = 6,
            CREATION_CONTEXT_BLOCK_FAILED = 7,

            CREATION_CONTEXT_FILE_PASSED = 8,
            CREATION_CONTEXT_FILE_FAILED = 9,

            CREATION_RUN_INFO_PASSED = 10,
            CREATION_RUN_INFO_FAILED = 11,

            SAVING_DATA_PASSED = 12,
            SAVING_DATA_FAILED = 13,

            CREATION_MADDOG_RUN_PASSED = 14,
            CREATION_MADDOG_RUN_FAILED = 15,

            MADDOG_RUN_STARTED_PASSED = 16,
            MADDOG_RUN_STARTED_FAILED = 17

            //COULDNOT_RETIEVE_PATCH_FILENAME

        };

        public enum LogLevel
        {
            ERROR = 1,
            WARNING = 2,
            INFORMATION = 3
        };

    }
}
