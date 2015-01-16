module.exports = function(grunt) {
  grunt.initConfig({
    pkg: grunt.file.readJSON("package.json"),
    coffee: {
      product: {
        options: {
          bare: true
        },
        expand: true,
        src: '*.coffee',
        dest: 'js/',
        ext: '.js'
      }
    },
    haml: {
      all: {
        files: {
          'index.html': 'index.haml'
        }
      }
    },
    watch: {
      css: {
        files: ["css/main.css"],
        options: {
          livereload: true
        }
      },
      haml: {
        files: ["*.haml"],
        tasks: ["haml:all"],
        options: {
          livereload: true
        }
      },
      coffee: {
        files: ["js/*.coffee"],
        tasks: ["coffee:product"],
        options: {
          livereload: true
        }
      }
    }
  });
  grunt.loadNpmTasks("grunt-contrib-coffee");
  grunt.loadNpmTasks("grunt-contrib-watch");
  grunt.loadNpmTasks("grunt-notify");
  grunt.loadNpmTasks("grunt-contrib-haml");
  grunt.registerTask("default", ["watch"]);
  return grunt.registerTask("ci", ["coffee"]);
};
