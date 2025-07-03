using BPS.EdPlanLoaderCore.MetaData;
using log4net;
using System.Xml;
using System.Xml.Linq;

namespace BPS.EdPlanLoaderCore.XMLDataLoad
{
    class ParseXmls
    {
        private readonly EdorgConfiguration _configuration = null;
        private readonly ILog _log;
        public ParseXmls(EdorgConfiguration configuration, ILog logger)
        {
            _configuration = configuration;
            _log = logger;
        }

        /// <summary>
        /// Converts an XDocument to an XmlDocument.
        /// </summary>
        /// <param name="xDocument">The source XDocument to convert.</param>
        /// <returns>An equivalent XmlDocument.</returns>
        public XmlDocument ToXmlDocument(XDocument xDocument)
        {
            // Create a new XmlDocument instance
            var xmlDocument = new XmlDocument();

            // Create an XmlReader from the XDocument and load it into the XmlDocument
            using (var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }
    }
}
