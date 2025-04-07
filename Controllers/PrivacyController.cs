using Microsoft.AspNetCore.Mvc;

namespace IntexBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrivacyController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetPrivacyPolicy()
        {
            var privacyPolicy = new
            {
                Title = "Privacy Policy",
                LastUpdated = "April 7, 2025",
                Sections = new[]
                {
                    new
                    {
                        Title = "Introduction",
                        Content = "This Privacy Policy explains how Movie Rating App ('we', 'us', 'our') collects, uses, and protects your personal information when you use our website and services. We are committed to ensuring the privacy and security of your data in compliance with the General Data Protection Regulation (GDPR) and other applicable privacy laws."
                    },
                    new
                    {
                        Title = "Information We Collect",
                        Content = "We collect the following types of information: Personal information (email address, name) when you create an account; Usage data (movies viewed, ratings submitted); Technical data (IP address, browser type, device information); Cookies and similar tracking technologies."
                    },
                    new
                    {
                        Title = "How We Use Your Information",
                        Content = "We use your information to: Provide and maintain our services; Personalize your experience; Process transactions; Send service-related communications; Improve our website and services; Comply with legal obligations."
                    },
                    new
                    {
                        Title = "Data Retention",
                        Content = "We retain your personal information only for as long as necessary to fulfill the purposes for which we collected it, including for the purposes of satisfying any legal, accounting, or reporting requirements."
                    },
                    new
                    {
                        Title = "Your Rights",
                        Content = "Under the GDPR, you have the right to: Access your personal data; Rectify inaccurate data; Erase your data ('right to be forgotten'); Restrict processing of your data; Data portability; Object to processing; Not be subject to automated decision-making."
                    },
                    new
                    {
                        Title = "Cookies",
                        Content = "We use cookies to enhance your browsing experience, analyze site traffic, and personalize content. You can control cookies through your browser settings."
                    },
                    new
                    {
                        Title = "Third-Party Services",
                        Content = "We may use third-party services that collect, monitor, and analyze data. These third parties have their own privacy policies addressing how they use such information."
                    },
                    new
                    {
                        Title = "Data Security",
                        Content = "We implement appropriate security measures to protect your personal information against unauthorized access, alteration, disclosure, or destruction."
                    },
                    new
                    {
                        Title = "Changes to This Privacy Policy",
                        Content = "We may update our Privacy Policy from time to time. We will notify you of any changes by posting the new Privacy Policy on this page and updating the 'Last Updated' date."
                    },
                    new
                    {
                        Title = "Contact Us",
                        Content = "If you have any questions about this Privacy Policy, please contact us at privacy@movierating.com."
                    }
                }
            };

            return Ok(privacyPolicy);
        }
    }
}
