﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Olive.Mvc.Microservices
{
    internal static class DevelopmentShareInfo
    {
        private static DevelopmentFullInfo SetFullInfo()
        {
            var navigations = NavigationApiMiddleWare.GetNavigationsFromAssembly<Navigation>();
            if (navigations.None()) return null;
            navigations.Do(r => r.Define());
            return new DevelopmentFullInfo()
            {
                BoardSources = navigations
                .SelectMany(x => x.GetType().GetMethods().Where(x => x.Name == "DefineDynamic"))
                .Select(x => x.GetParameters().LastOrDefault().ParameterType.Name)
                .Distinct().ToArray(),
                Features = navigations.Select(x => x.GetFeatures()).SelectMany(x => x).ToArray(),
                Service = new Service()
                {
                    Name = Microservice.Me.Name,
                    BaseUrl = Microservice.Me.Url(),
                    Icon = "fas fa-" + Microservice.Me.Name.ToLower(),
                },
                GlobalySearchable = GetControllerFromAssembly<Olive.GlobalSearch.SearchSource>().HasAny(),
            };


        }
        static Type[] AllLoadedTypes() => AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).ToArray();
        private static IEnumerable<T> GetControllerFromAssembly<T>() where T : Olive.GlobalSearch.SearchSource
        {
            var types = AllLoadedTypes();
            types = types.Where(x => x.IsA<Olive.GlobalSearch.SearchSource>() && !x.IsAbstract).ToArray();
            return types.Select(t => (T)Activator.CreateInstance(t)).ToArray();
        }
        internal static async Task ShareMyData()
        {
            var info = SetFullInfo();
            if (info == null) return;
            try
            {
                var uri = (Microservice.Of("hub") + "/LocalSetup").AsUri();
                await uri.PostJson(Newtonsoft.Json.JsonConvert.SerializeObject(info));
            }
            catch (Exception ex)
            {
                Log.For(typeof(DevelopmentShareInfo)).Error("Could not reach local hub.\n" + ex);
            }
        }
    }
}