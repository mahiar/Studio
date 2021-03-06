﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBusStudio.Automation.Extensions;
using Microsoft.VisualStudio.TeamArchitect.PowerTools;
using Microsoft.VisualStudio.Patterning.Runtime;
using AbstractEndpoint;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.Patterning.Extensibility.References;
using System.IO;
using EnvDTE;
using System.ComponentModel.Composition;
using NServiceBusStudio.Automation.Infrastructure;


namespace NServiceBusStudio
{
    partial interface IComponent : IProjectReferenced
    {
        void EndpointDefined(IAbstractEndpoint endpoint);
        IEnumerable<IAbstractEndpoint> DeployedTo { get; }
        void DeployTo(IAbstractEndpoint endpoint);
        void Publish(IEvent @event);
        void Subscribe(ICommand command);
        void RemoveLinks(IAbstractEndpoint endpoint);
    }

    partial class Component : IValidatableObject, IRenameRefactoring
    {
        public IProject Project
        {
            get { return this.AsElement().GetProject(); }
        }

        partial void Initialize()
        {
            this.AsElement().Deleting += (s, e) =>
            {
                foreach (var endpoint in this.DeployedTo)
                {
                    DeleteComponentLink(endpoint);   
                }
            };
        }

        private void DeleteComponentLink(IAbstractEndpoint endpoint)
        {
            var componentLink = endpoint.EndpointComponents.AbstractComponentLinks.FirstOrDefault(cl => cl.ComponentReference.Value == this);
            componentLink.As<IProductElement>().Delete();
        }

        private List<IAbstractEndpoint> deployedTo = new List<IAbstractEndpoint>();
        public IEnumerable<IAbstractEndpoint> DeployedTo
        {
            get
            {
                try
                {
                    return deployedTo ??
                        this.As<IProductElement>().Root.As<IApplication>().Design.Endpoints.GetAll()
                        .Where(ep => ep.EndpointComponents.AbstractComponentLinks.Any(cl => cl.ComponentReference.Value == this));
                }
                catch
                {
                    return null;
                }
            }
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var result = new List<ValidationResult>();
            var root = this.As<IProductElement>().Root;
            if (!(root.As<NServiceBusStudio.IApplication>().Design.Endpoints.GetAll()
                .Any(ep => ep.EndpointComponents.AbstractComponentLinks.Any(cl => cl.ComponentReference.Value == this))))
            {
                result.Add(new ValidationResult(string.Format("{0}.{1} should be allocated to an endpoint.", this.Parent.Parent.InstanceName, this.InstanceName)));
            }
            return result;
        }

        public void EndpointDefined(IAbstractEndpoint newEndpoint)
        {
            if (newEndpoint != null && !this.deployedTo.Contains(newEndpoint))
            {
                this.deployedTo.Add(newEndpoint);
            }

            foreach (var endpoint in this.DeployedTo)
            {
                var service = this.Parent.Parent;
                if (!endpoint.Project.Folders.Any(f => f.Name == service.CodeIdentifier))
                {
                    endpoint.Project.CreateFolder(service.CodeIdentifier);
                }

                foreach (var i in this.Subscribes.SubscribedEventLinks)
                {
                    i.AsElement().AutomationExtensions.First(x => x.Name == "OnInstantiateCommand").Execute();
                }
                foreach (var i in this.Subscribes.ProcessedCommandLinks)
                {
                    i.AsElement().AutomationExtensions.First(x => x.Name == "OnInstantiateCommand").Execute();
                }
                foreach (var i in this.Publishes.CommandLinks)
                {
                    i.AsElement().AutomationExtensions.First(x => x.Name == "OnInstantiateCommand").Execute();
                }
                foreach (var i in this.Publishes.EventLinks)
                {
                    i.AsElement().AutomationExtensions.First(x => x.Name == "OnInstantiateCommand").Execute();
                }

                // Generate Code for Component Handlers
                this.AsElement().AutomationExtensions.First(x => x.Name == "GenerateCodeHandlers").Execute();
                this.AsElement().AutomationExtensions.First(x => x.Name == "UnfoldCustomHandlers").Execute();
                if (this.IsSaga)
                {
                    this.AsElement().AutomationExtensions.First(x => x.Name == "UnfoldSagaDataCode").Execute();
                }

                // Add Links for Referenced Libraries
                this.AddLinks(endpoint);
            }
        }

        private void AddLinks(IAbstractEndpoint endpoint)
        {
            var project = endpoint.Project;

            // 1. Add Links for Custom Code
            var customCodePath = endpoint.CustomizationFuncs().BuildPathForComponentCode(endpoint, this.Parent.Parent, null);
            AddLinkToProject(project, this.As<IProductElement>(), customCodePath, 
                items => items.First(i => 
                    Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(i.PhysicalPath))) != "Infrastructure"
                ));

            //2. Add Links for Infrastructure Code
            var infraCodePath = endpoint.CustomizationFuncs().BuildPathForComponentCode(endpoint, this.Parent.Parent, "Infrastructure");
            AddLinkToProject(project, this.As<IProductElement>(), infraCodePath,
                items => items.First(i =>
                    Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(i.PhysicalPath))) == "Infrastructure"
                ));

            // 3. Add Links for References
            foreach (var libraryLink in this.LibraryReferences.LibraryReference)
            {
                IProductElement element = null;

                if (libraryLink.Library != null)
                {
                    if (libraryLink.Library.As<IProductElement>().References.Any(r => r.Kind == ReferenceKindConstants.ArtifactLink))
                    {
                        element = libraryLink.Library.As<IProductElement>();
                    }
                }
                else
                {
                    if (libraryLink.ServiceLibrary.As<IProductElement>().References.Any(r => r.Kind == ReferenceKindConstants.ArtifactLink))
                    {
                        element = libraryLink.ServiceLibrary.As<IProductElement>();
                    }
                }

                var suggestedPath = endpoint.CustomizationFuncs().BuildPathForComponentCode(endpoint, this.Parent.Parent, null);

                AddLinkToProject(project, element, suggestedPath, i => i.First());
            }
        }

        public void RemoveLinks(IAbstractEndpoint endpoint)
        {
            var project = endpoint.Project;

            if (project == null)
                return;

            // 1. Remove Links for Custom Code
            var customCodePath = endpoint.CustomizationFuncs().BuildPathForComponentCode(endpoint, this.Parent.Parent, null);
            RemoveLinkFromProject(project, this.InstanceName + ".cs", customCodePath);

            //2. Remove Links for Infrastructure Code
            var infraCodePath = endpoint.CustomizationFuncs().BuildPathForComponentCode(endpoint, this.Parent.Parent, "Infrastructure");
            RemoveLinkFromProject(project, this.InstanceName + ".cs", infraCodePath);

            // 3. Remove Links for References
            foreach (var libraryLink in this.LibraryReferences.LibraryReference)
            {
                IProductElement element = null;

                if (libraryLink.Library != null)
                {
                    if (libraryLink.Library.As<IProductElement>().References.Any(r => r.Kind == ReferenceKindConstants.ArtifactLink))
                    {
                        element = libraryLink.Library.As<IProductElement>();
                    }
                }
                else
                {
                    if (libraryLink.ServiceLibrary.As<IProductElement>().References.Any(r => r.Kind == ReferenceKindConstants.ArtifactLink))
                    {
                        element = libraryLink.ServiceLibrary.As<IProductElement>();
                    }
                }

                var suggestedPath = endpoint.CustomizationFuncs().BuildPathForComponentCode(endpoint, this.Parent.Parent, null);

                RemoveLinkFromProject(project, element.InstanceName + ".cs", suggestedPath);
            }
        }

        private static void AddLinkToProject(IProject project, IProductElement element, string suggestedPath, Func<IEnumerable<IItem>, IItem> filter)
        {
            var sourceFile = FindSourceItemForElement(element, filter);
            var container = project.As<EnvDTE.Project>().ProjectItems;
            container = FindProjectFolder(container, suggestedPath);

            if (container != null)
            {
                try
                {
                    container.AddFromFile(sourceFile.PhysicalPath);
                }
                catch { } // If the link is already in place we will ignore the exception
            }
        }

        private void RemoveLinkFromProject(IProject project, string fileName, string suggestedPath)
        {
            var container = project.As<EnvDTE.Project>().ProjectItems;
            container = FindProjectFolder(container, suggestedPath);

            if (container != null)
            {
                foreach (var file in container)
                {
                    if (file != null && file.As<EnvDTE.ProjectItem>().Name == fileName)
                    {
                        file.As<ProjectItem>().Delete();
                        break;
                    }
                }
            }
        }


        private static IItem FindSourceItemForElement(IProductElement element, Func<IEnumerable<IItem>, IItem> filter)
        {
            var references = Microsoft.VisualStudio.TeamArchitect.PowerTools.Features.ServiceProviderExtensions
                .GetService<IFxrUriReferenceService>(element.Product.ProductState);

            var sourceFile = filter(SolutionArtifactLinkReference.GetResolvedReferences(element, references).OfType<IItem>());
            return sourceFile;
        }

        private static EnvDTE.ProjectItems FindProjectFolder(EnvDTE.ProjectItems container, string suggestedPath)
        {
            var path = suggestedPath.Split('\\').Skip(1);

            foreach (var stepName in path)
            {
                foreach (var folder in container)
                {
                    if (folder != null && folder.As<EnvDTE.ProjectItem>().Name == stepName)
                    {
                        container = folder.As<EnvDTE.ProjectItem>().ProjectItems;
                        break;
                    }
                }
            }
            
            return container;
        }

        // Looks for an available component name in the service
        public static string TryGetComponentName(string suggested, IService service)
        {
            var candidate = suggested;
            for (int i = 1; true; i++)
            {
                if (!service.Components.Component.Any(c => c.InstanceName == candidate))
                {
                    break;
                }
                candidate = suggested + i.ToString().Trim();
            }

            return candidate;
        }

        public void DeployTo(IAbstractEndpoint endpoint)
        {
            if (!endpoint.EndpointComponents.AbstractComponentLinks.Any(cl => cl.ComponentReference.Value == this))
            {
                var linkSource = endpoint.EndpointComponents.CreateComponentLink(String.Format("{0}.{1}", this.Parent.Parent.InstanceName, this.InstanceName), e => e.ComponentReference.Value = this);
                linkSource.ComponentReference.Value.EndpointDefined(endpoint);
            }
        }
       
        public void Publish(IEvent @event)
        {
            if (!this.Publishes.EventLinks.Any(el => el.EventReference.Value == @event))
            {
                var linkSource = this.Publishes.CreateEventLink(String.Format("{0}.{1}", @event.Parent.Parent.Parent.InstanceName, @event.InstanceName), e => e.EventReference.Value = @event);
            }
        }

        public void Subscribe(ICommand command)
        {
            if (!this.Subscribes.ProcessedCommandLinks.Any(cl => cl.CommandReference.Value == command))
            {
                var linkSource = this.Subscribes.CreateProcessedCommandLink(String.Format("{0}.{1}", command.Parent.Parent.Parent.InstanceName, command.InstanceName), e => e.CommandReference.Value = command);
            }
        }
    }
}
