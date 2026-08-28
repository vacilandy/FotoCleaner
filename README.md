# FotoCleaner

Aplicacion WPF nativa para Windows 10/11 (.NET 8) que encuentra fotos duplicadas o visualmente similares en todos los formatos de imagen compatibles.

## Arquitectura

- `ViewModels`: estado y comandos MVVM.
- `Services/PerceptualHashService`: dHash 64-bit para imagenes. Los videos se descartan.
- `Services/MediaScanner`: enumeracion segura, paralelismo y cache.
- `Services/HashDatabase`: SQLite en `%LOCALAPPDATA%/FotoCleaner/cache.db`.
- `Services/FileRelocationService`: mueve seleccionados a `Duplicadas`, sin borrar permanentemente.

## Verificacion

Requiere instalar el SDK .NET 8 (el runtime no basta):

```powershell
dotnet restore
dotnet build -c Release
dotnet run
```

Prueba manual: selecciona una carpeta con copias redimensionadas o con distinto formato, ajusta el umbral, analiza y selecciona archivos. El segundo analisis reutiliza la cache. Confirma que los seleccionados aparecen en `<carpeta>\\Duplicadas`.

La aplicacion solo analiza la carpeta elegida y sus descendientes, normaliza rutas con `Path.GetFullPath` y valida que los movimientos permanezcan dentro de esa raiz.

## Recomendaciones de rendimiento

- La cache SQLite evita recalcular fotos que no cambiaron; el guardado de nuevos hashes se realiza en una transaccion por lote.
- El escaneo usa concurrencia limitada para equilibrar CPU y velocidad del disco.
- Las listas usan reciclado de filas y las miniaturas se decodifican reducidas para mantener fluido el desplazamiento.
- Para bibliotecas muy grandes, analiza por subcarpetas y usa miniaturas de 120 a 180 px.

## Crear un instalador

La carpeta `publish-definitive` contiene el ejecutable autocontenido para Windows x64. Para distribuirlo como instalador, una opción sencilla es Inno Setup:

1. Instala Inno Setup desde https://jrsoftware.org/isinfo.php.
2. Crea un nuevo script y usa como carpeta de archivos la carpeta `publish-definitive`.
3. Define `FotoCleaner.exe` como aplicación principal y crea el instalador.

El instalador puede incluir un acceso directo en el escritorio y en el menu Inicio. La carpeta de datos de la aplicacion se guarda en `%LOCALAPPDATA%/FotoCleaner` y contiene la cache SQLite.
