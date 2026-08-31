namespace CASTIglesias
{
    /// <summary>
    /// Clase marcadora (sin miembros) que identifica el archivo de recursos compartido
    /// de la aplicación. Se usa como parámetro de tipo en <c>IStringLocalizer&lt;SharedResource&gt;</c>
    /// e <c>IHtmlLocalizer&lt;SharedResource&gt;</c>, y ASP.NET Core la traduce a la ruta
    /// del .resx correspondiente.
    ///
    /// IMPORTANTE: esta clase debe vivir en la RAÍZ del proyecto, con el namespace
    /// raíz (CASTIglesias) y no dentro de la carpeta Resources.
    ///
    /// El motivo es cómo ResourceManagerStringLocalizerFactory compone la ruta:
    /// concatena [namespace raíz] + [ResourcesPath] + [nombre del tipo sin el namespace raíz].
    ///
    ///   Clase en CASTIglesias            -> CASTIglesias + Resources. + SharedResource
    ///                                    -> busca Resources/SharedResource.en.resx      (CORRECTO)
    ///
    ///   Clase en CASTIglesias.Resources  -> CASTIglesias.Resources. + Resources.SharedResource
    ///                                    -> busca Resources/Resources/SharedResource.en.resx (NO EXISTE)
    ///
    /// Si se mueve esta clase a la carpeta Resources, la localización deja de
    /// encontrar traducciones y todo sale en español sin error visible, porque el
    /// localizador devuelve la clave cuando no halla el recurso.
    /// </summary>
    public class SharedResource
    {
    }
}
