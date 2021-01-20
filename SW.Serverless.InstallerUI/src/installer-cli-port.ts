import {exec} from "child_process"
import fs from "fs"
import path from "path"
import archiver from "archiver"
import {Connection} from "./model"

const buildPublish = (projectPath: string, outputPath: string, callback: (outPath: string, zipPath: string) => void ) => {
  exec(`dotnet publish "${projectPath}" -o "${outputPath}"`, (err, stdout, stderr) => {
    if(!err){
      callback(outputPath, path.join(__dirname, "../build.zip"));
    }
    else {
      console.log(err);
      console.log(stdout);
      console.log(stderr);
    }
  } )
}

const compress = (path: string, zipFileName: string, callback: (zipPath: string) => void) => {
  const output = fs.createWriteStream(zipFileName);
  const archive = archiver('zip', {
    gzip: true,
    zlib: {level: 9}
  });
  archive.pipe(output);
  const files = fs.readdirSync(path)
  files.forEach(f => {
    archive.file(f, {
      name: f
    });
    archive.finalize();
  })

  archive.on('end', () => {
    callback(zipFileName);
  } )
}

const pushToCloud = (zipPath: string, adapterId: string, entryAssembly: string, connection: Connection ) => {

}


