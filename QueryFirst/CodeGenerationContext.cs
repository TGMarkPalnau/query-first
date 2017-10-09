﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyIoC;
using EnvDTE;
using System.IO;
using System.Text.RegularExpressions;
using System.Configuration;
using QueryFirst.TypeMappings;

namespace QueryFirst
{
    public class CodeGenerationContext
    {
        protected TinyIoCContainer tiny;
        private PutCodeHere _putCodeHere;
        public PutCodeHere PutCodeHere { get { return _putCodeHere; } }
        protected DTE dte;
        public DTE Dte { get { return dte; } }
        protected Document queryDoc;
        public Document QueryDoc { get { return queryDoc; } }
        protected IProvider provider;
        public IProvider Provider { get { return provider; } }
        protected Query query;

        // constructor
        public CodeGenerationContext(Document queryDoc)
        {
            tiny = TinyIoCContainer.Current;
            queryHasRun = false;
            this.queryDoc = queryDoc;
            dte = queryDoc.DTE;
            query = new Query(this);
            provider = tiny.Resolve<IProvider>(DesignTimeConnectionString.v.ProviderName);
            provider.Initialize(DesignTimeConnectionString.v);
            // resolving the target project item for code generation. We know the file name, we loop through child items of the query til we find it.
            _putCodeHere = new PutCodeHere(Conductor.GetItemByFilename(queryDoc.ProjectItem.ProjectItems, GeneratedClassFullFilename));


            string currDir = Path.GetDirectoryName(queryDoc.FullName);

            hlpr = new AdoSchemaFetcher();

			_resultClassNameSuffix();

		}
        public Query Query { get { return query; } }
        protected string baseName;
        private ConfigurationAccessor _config;
        public ConfigurationAccessor ProjectConfig
        {
            get
            {
                if (_config == null)
                {
                    try
                    {
                        _config = new ConfigurationAccessor(dte, null);
                    }
                    catch (Exception ex)
                    {
                        // will throw if there's no configuration file
                        return null;
                    }
                }
                return _config;
            }
        }
        /// <summary>
        /// The name of the query file, without extension. Used to infer the filenames of code classes, and to generate the wrapper class name.
        /// </summary>
        public string BaseName
        {
            get
            {
                if (baseName == null)
                    baseName = Path.GetFileNameWithoutExtension((string)queryDoc.ProjectItem.Properties.Item("FullPath").Value);
                return baseName;
            }
        }
        /// <summary>
        /// The directory containing the 3 files for this query, with trailing slash
        /// </summary>
        public string CurrDir { get { return Path.GetDirectoryName((string)queryDoc.ProjectItem.Properties.Item("FullPath").Value) + "\\"; } }
        /// <summary>
        /// The query filename, extension and path relative to approot. Used for generating the call to GetManifestStream()
        /// </summary>
        public string PathFromAppRoot
        {
            get
            {
                string fullNameAndPath = (string)queryDoc.ProjectItem.Properties.Item("FullPath").Value;
                return fullNameAndPath.Substring(queryDoc.ProjectItem.ContainingProject.Properties.Item("FullPath").Value.ToString().Length);
            }
        }
        /// <summary>
        /// DefaultNamespace.Path.From.Approot.QueryFileName.sql
        /// </summary>
        public string NameAndPathForManifestStream
        {
            get
            {
                EnvDTE.Project vsProject = queryDoc.ProjectItem.ContainingProject;
                return vsProject.Properties.Item("DefaultNamespace").Value.ToString() + '.' + PathFromAppRoot.Replace('\\', '.');
            }
        }
        /// <summary>
        /// Path and filename of the generated code file.
        /// </summary>
        public string GeneratedClassFullFilename
        {
            get
            {
                return CurrDir + BaseName + ".gen.cs";
            }
        }
        protected string userPartialClass;
        protected string resultClassName;

		public string resultClassNameSuffix = null;
		private void _resultClassNameSuffix()
		{
			string classNameSuffix = "Model";

			resultClassNameSuffix = classNameSuffix;
		}
		
        /// <summary>
        /// Result class name, read from the user's half of the partial class, written to the generated half.
        /// </summary>
        public virtual string ResultClassName
        {
            get
            {
                if (string.IsNullOrEmpty(userPartialClass))
                    userPartialClass = File.ReadAllText(CurrDir + BaseName + resultClassNameSuffix + ".cs");
                if (resultClassName == null)
                    resultClassName = Regex.Match(userPartialClass, "(?im)partial class (\\S+)").Groups[1].Value;
                return resultClassName;

            }
        }
        /// <summary>
        /// The query namespace, read from the user's half of the result class, used for the generated code file.
        /// </summary>
        public virtual string Namespace
        {
            get
            {
                if (string.IsNullOrEmpty(userPartialClass))
                    userPartialClass = File.ReadAllText(CurrDir + BaseName + resultClassNameSuffix + ".cs");
                return Regex.Match(userPartialClass, "(?im)^namespace (\\S+)").Groups[1].Value;

            }
        }
        protected DesignTimeConnectionString _dtcs;
        public DesignTimeConnectionString DesignTimeConnectionString
        {
            get
            {
                return _dtcs ?? (_dtcs = new DesignTimeConnectionString(this));
            }
        }

        protected string methodSignature;
        /// <summary>
        /// Parameter types and names, with trailing comma.
        /// </summary>
        public virtual string MethodSignature
        {
            // todo this should be a stringtemplate
            get
            {
                if (string.IsNullOrEmpty(methodSignature))
                {
                    StringBuilder sig = new StringBuilder();
                    int i = 0;
                    foreach (var qp in Query.QueryParams)
                    {
                        sig.Append(qp.CSType + ' ' + qp.CSName + ", ");
                        i++;
                    }
                    //signature trailing comma trimmed in place if not needed. 
                    methodSignature = sig.ToString();
                }
                return methodSignature;
            }
        }
		protected string queryModel = "QueryModel";
        //taken out of constructor, we don't need this anymore????
        //                ((ISignatureMaker)TinyIoCContainer.Current.Resolve(typeof(ISignatureMaker)))
        //.MakeMethodAndCallingSignatures(ctx.Query.QueryParams, out methodSignature, out callingArgs);
        protected string callingArgs;
        /// <summary>
        /// Parameter names, if any, with trailing "conn". String used by connectionless methods to call their connectionful overloads.
        /// </summary>
        public string CallingArgs
        {
            get
            {
                if (string.IsNullOrEmpty(callingArgs))
                {
					bool useObject = false;
					System.Configuration.KeyValueConfigurationElement useParametersObject = null;
					try
					{
						useParametersObject = ProjectConfig.AppSettings["QfUseParametersObject"];
					}
					catch (Exception){}//nobody cares
					if (useParametersObject != null)
					{
						try
						{
							if (Convert.ToBoolean(useParametersObject.Value))
								useObject = true;
						}
						catch (Exception){}//still, nobody cares
					}

					StringBuilder call = new StringBuilder();
					if (useObject)
					{
						call.Append(BaseName + queryModel + " " + queryModel + ", ");
					}
					else
					{
						foreach (var qp in Query.QueryParams)
						{
							//sig.Append(qp.CSType + ' ' + qp.CSName + ", ");
							call.Append(qp.CSName + ", ");
						}
					}
                    
                    //signature trailing comma trimmed in place if needed. 
                    call.Append("conn"); // calling args always used to call overload with connection
                    callingArgs = call.ToString();
                }
                return callingArgs;
            }
        }
        protected List<ResultFieldDetails> resultFields;
        /// <summary>
        /// The schema table returned from the dummy run of the query.
        /// </summary>
        public List<ResultFieldDetails> ResultFields
        {
            get { return resultFields; }
            set { resultFields = value; }
        }

        protected bool queryHasRun;
        public bool QueryHasRun
        {
            get { return queryHasRun; }
            set { queryHasRun = value; }
        }
        protected AdoSchemaFetcher hlpr;
        /// <summary>
        /// The class that runs the query and returns the schema table
        /// </summary>
        public AdoSchemaFetcher Hlpr { get { return hlpr; } }
        protected ProjectItem resultsClass;



    }
}
