# TemplateEz -- Simply generate templated text in a C# program

<a href="https://github.com/EngagingGear/TemplateEz/wiki/TemplateEz----How-to-easily-generate-templated-output">Documentation is here.</a>

Often when in our programs we want to generate a piece of text that is based on a template, but the details vary depending on the specific data. For example, we might want to generate a form email inserting the user’s name, address etc., or perhaps generate an invoice and save it in a database. Also, we might want to use a template engine to generate code files for automatic program generation, and there are a myriad other uses.

There are a few options for doing this available in the Microsoft world. The most obvious one would be Razor, and engine used in ASP.NET for generating such files, or T4, a template generation engine. However, although these are both good options they are actually quite difficult to use. Razor requires you to embed ASP.NET into your program, and T4 is extremely complicated to use, and has an ugly hard to understand syntax.

The purpose of TemplateEz it to make templates EXTREMELY easy to use. You provide a template, which is compiled, and then you call Execute to generate the data.

<table>
  <tr>
    <td width="66%">
      This software is sponsored by Surge, a Catalyte company. I gratefully acknowledge their support and help.
      If you are a great developer looking for a great consulting company you should check them out.
      And if you are looking to bring on developers to your team you will find the very highest quality talent there.
      <a href="https://www.surgeforward.com/">Click here for more information.</a>
    </td>
  <td>
    <a href="https://www.surgeforward.com/">
      <img src="https://user-images.githubusercontent.com/88802169/144468950-d0a8fadf-8fa8-4893-801c-f543e11da9e9.png" />
    </a>
   </td>
 </tr>
</table>
