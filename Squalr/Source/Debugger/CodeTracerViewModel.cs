﻿namespace Squalr.Source.Debugger
{
    using GalaSoft.MvvmLight.CommandWpf;
    using Squalr.Engine;
    using Squalr.Engine.Debugger;
    using Squalr.Engine.Utils;
    using Squalr.Engine.Utils.DataStructures;
    using Squalr.Source.Docking;
    using Squalr.Source.ProjectExplorer;
    using Squalr.Source.ProjectItems;
    using Squalr.Source.Utils.Extensions;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// View model for the Code Tracer.
    /// </summary>
    internal class CodeTracerViewModel : ToolViewModel
    {
        /// <summary>
        /// Singleton instance of the <see cref="CodeTracerViewModel" /> class.
        /// </summary>
        private static Lazy<CodeTracerViewModel> codeTracerViewModelInstance = new Lazy<CodeTracerViewModel>(
                () => { return new CodeTracerViewModel(); },
                LazyThreadSafetyMode.ExecutionAndPublication);

        private FullyObservableCollection<CodeTraceResult> results;

        /// <summary>
        /// The selected code trace results.
        /// </summary>
        private IEnumerable<CodeTraceResult> selectedCodeTraceResults;

        /// <summary>
        /// Prevents a default instance of the <see cref="CodeTracerViewModel" /> class from being created.
        /// </summary>
        private CodeTracerViewModel() : base("Code Tracer")
        {
            DockingViewModel.GetInstance().RegisterViewModel(this);

            this.Results = new FullyObservableCollection<CodeTraceResult>();

            this.FindWhatWritesCommand = new RelayCommand<ProjectItem>((projectItem) => this.FindWhatWrites(projectItem));
            this.FindWhatReadsCommand = new RelayCommand<ProjectItem>((projectItem) => this.FindWhatReads(projectItem));
            this.FindWhatAccessesCommand = new RelayCommand<ProjectItem>((projectItem) => this.FindWhatAccesses(projectItem));
            this.StopCommand = new RelayCommand(() => this.StopTrace());
            this.SelectInstructionCommand = new RelayCommand<Object>((selectedItems) => this.SelectedCodeTraceResults = (selectedItems as IList)?.Cast<CodeTraceResult>(), (selectedItems) => true);
            this.AddInstructionCommand = new RelayCommand<CodeTraceResult>((codeTraceResult) => this.AddCodeTraceResult(codeTraceResult));
            this.AddInstructionsCommand = new RelayCommand<Object>((selectedItems) => this.AddCodeTraceResults(this.SelectedCodeTraceResults));
        }

        /// <summary>
        /// Gets a command to find what writes to an address.
        /// </summary>
        public ICommand FindWhatWritesCommand { get; private set; }

        /// <summary>
        /// Gets a command to find what reads from an address.
        /// </summary>
        public ICommand FindWhatReadsCommand { get; private set; }

        /// <summary>
        /// Gets a command to find what accesses an an address.
        /// </summary>
        public ICommand FindWhatAccessesCommand { get; private set; }

        /// <summary>
        /// Gets a command to stop recording events.
        /// </summary>
        public ICommand StopCommand { get; private set; }

        /// <summary>
        /// Gets or sets the command to select scan results.
        /// </summary>
        public ICommand SelectInstructionCommand { get; private set; }

        /// <summary>
        /// Gets the command to add a scan result to the project explorer.
        /// </summary>
        public ICommand AddInstructionCommand { get; private set; }

        /// <summary>
        /// Gets the command to add all selected scan results to the project explorer.
        /// </summary>
        public ICommand AddInstructionsCommand { get; private set; }

        public FullyObservableCollection<CodeTraceResult> Results
        {
            get
            {
                return this.results;
            }

            set
            {
                this.results = value;
            }
        }

        /// <summary>
        /// Gets or sets the selected code trace results.
        /// </summary>
        public IEnumerable<CodeTraceResult> SelectedCodeTraceResults
        {
            get
            {
                return this.selectedCodeTraceResults;
            }

            set
            {
                this.selectedCodeTraceResults = value;
                this.RaisePropertyChanged(nameof(this.SelectedCodeTraceResults));
            }
        }

        /// <summary>
        /// Gets a singleton instance of the <see cref="DebuggerViewModel"/> class.
        /// </summary>
        /// <returns>A singleton instance of the class.</returns>
        public static CodeTracerViewModel GetInstance()
        {
            return codeTracerViewModelInstance.Value;
        }

        /// <summary>
        /// Adds the given code trace result to the project explorer.
        /// </summary>
        /// <param name="codeTraceResult">The code trace result to add to the project explorer.</param>
        private void AddCodeTraceResult(CodeTraceResult codeTraceResult)
        {
            InstructionItem instructionItem = new InstructionItem(codeTraceResult.Address.ToIntPtr(), "", "nop", new Byte[] { 0x90 });

            ProjectExplorerViewModel.GetInstance().AddNewProjectItems(addToSelected: true, projectItems: instructionItem);
        }

        /// <summary>
        /// Adds the given code trace results to the project explorer.
        /// </summary>
        /// <param name="codeTraceResults">The code trace results to add to the project explorer.</param>
        private void AddCodeTraceResults(IEnumerable<CodeTraceResult> codeTraceResults)
        {
            if (codeTraceResults == null)
            {
                return;
            }

            IEnumerable<InstructionItem> projectItems = codeTraceResults.Select(
                codeTraceEvent => new InstructionItem(codeTraceEvent.Address.ToIntPtr(), "", "nop", new Byte[] { 0x90 }));

            ProjectExplorerViewModel.GetInstance().AddNewProjectItems(addToSelected: true, projectItems: projectItems);
        }

        private void StopTrace()
        {

        }

        private void FindWhatWrites(ProjectItem projectItem)
        {
            if (projectItem is AddressItem)
            {
                AddressItem addressItem = projectItem as AddressItem;

                BreakpointSize size = Eng.GetInstance().Debugger.SizeToBreakpointSize((UInt32)Conversions.SizeOf(addressItem.DataType));
                Eng.GetInstance().Debugger.FindWhatWrites(addressItem.CalculatedAddress.ToUInt64(), size, this.CodeTraceEvent);
            }
        }

        private void FindWhatReads(ProjectItem projectItem)
        {
            if (projectItem is AddressItem)
            {
                AddressItem addressItem = projectItem as AddressItem;

                BreakpointSize size = Eng.GetInstance().Debugger.SizeToBreakpointSize((UInt32)Conversions.SizeOf(addressItem.DataType));
                Eng.GetInstance().Debugger.FindWhatReads(addressItem.CalculatedAddress.ToUInt64(), size, this.CodeTraceEvent);
            }
        }

        private void FindWhatAccesses(ProjectItem projectItem)
        {
            if (projectItem is AddressItem)
            {
                AddressItem addressItem = projectItem as AddressItem;

                BreakpointSize size = Eng.GetInstance().Debugger.SizeToBreakpointSize((UInt32)Conversions.SizeOf(addressItem.DataType));
                Eng.GetInstance().Debugger.FindWhatAccesses(addressItem.CalculatedAddress.ToUInt64(), size, this.CodeTraceEvent);
            }
        }

        private void CodeTraceEvent(CodeTraceInfo codeTraceInfo)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                CodeTraceResult result = this.Results.FirstOrDefault(results => results.Address == codeTraceInfo.Address);

                // Insert or increment
                if (result != null)
                {
                    result.Count++;
                }
                else
                {
                    this.Results.Add(new CodeTraceResult(codeTraceInfo));
                }
            }));
        }
    }
    //// End class
}
//// End namespace