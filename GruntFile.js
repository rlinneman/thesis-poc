module.exports = function (grunt){

	grunt.initConfig({
        ngdocs:{
            all:['src/ThesisPortal/app/**/*.js']
        },
        watch: {
          scripts: {
            files: ['GruntFile.js', 'src/ThesisPortal/app/**/*.js'],
            tasks: ['ngdocs', 'jshint']
          }
        },
        clean: ['docs'],
        jshint:{
            files: {src:['src/ThesisPortal/app/**/*.js']},
            options:{
              "curly": true,
              "eqnull": true,
              "eqeqeq": true,
              "undef": true,
              "globals": {
                "angular": true,
                "console":true
              }
            }
        }
    });

    grunt.loadNpmTasks('grunt-ngdocs');
    grunt.loadNpmTasks('grunt-contrib-watch');
    grunt.loadNpmTasks('grunt-contrib-clean');
    grunt.loadNpmTasks('grunt-contrib-jshint');
    
    grunt.registerTask('default', 'Default Task Alias', ['docs']);
	grunt.registerTask('docs', 'documentation', ['clean','ngdocs']);
};
