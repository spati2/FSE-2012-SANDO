﻿using System.Threading;
using EnvDTE80;
using Sando.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sando.Validation
{
    public static class TestValidator
    {
        private static List<Tuple<String, String>> _testNameLibraryList;
        private static bool _updatedTestList = false;
        private static VsTestConsoleRunner _testRunner;
        private static IntelliTraceConsoleRunner _intellitraceRunner;

        public static void Initialize()
        {
            var dte = ServiceLocator.Resolve<DTE2>();
            _testRunner = new VsTestConsoleRunner(dte);
            _intellitraceRunner = new IntelliTraceConsoleRunner(dte);

            var testDiscoveryTask = Task.Factory.StartNew(() =>
            {
                _testNameLibraryList = _testRunner.DiscoverTests();
            }, new CancellationToken(false));

            testDiscoveryTask.ContinueWith((continuation) =>
            {
                _updatedTestList = true;

            });
        }

        public static List<Tuple<String, String>> GetTestList()
        {
            return _updatedTestList ? _testNameLibraryList : null;
        }

        public static void FilterResultsUsingTestExecution(String selectedTest)
        {
            var validateResultsTask = Task.Factory.StartNew(() =>
            {
                //execute intellitrace
                _intellitraceRunner.SelectResultsUsingIntelliTrace(selectedTest);

            }, new CancellationToken(false));

            validateResultsTask.ContinueWith((continuation) =>
            {
                //highlight results in UI
                //hide circular progress bar
            });
        }

    }
}