# This file is auto-generated from the current state of the database. Instead
# of editing this file, please use the migrations feature of Active Record to
# incrementally modify your database, and then regenerate this schema definition.
#
# Note that this schema.rb definition is the authoritative source for your
# database schema. If you need to create the application database on another
# system, you should be using db:schema:load, not running all the migrations
# from scratch. The latter is a flawed and unsustainable approach (the more migrations
# you'll amass, the slower it'll run and the greater likelihood for issues).
#
# It's strongly recommended that you check this file into your version control system.

ActiveRecord::Schema.define(version: 20161106041404) do

  create_table "expression_averages", force: :cascade do |t|
    t.float    "expression_avg"
    t.datetime "created_at",     null: false
    t.datetime "updated_at",     null: false
  end

  create_table "user_expressions", force: :cascade do |t|
    t.integer  "expression"
    t.binary   "face_img"
    t.integer  "user_id"
    t.datetime "created_at", null: false
    t.datetime "updated_at", null: false
    t.index ["user_id"], name: "index_user_expressions_on_user_id"
  end

  create_table "users", force: :cascade do |t|
    t.string   "login_id",                       null: false
    t.string   "student_name"
    t.string   "password",                       null: false
    t.boolean  "connected_flag", default: false, null: false
    t.boolean  "teacher_flag",   default: false, null: false
    t.boolean  "admin_flag",     default: false, null: false
    t.datetime "created_at",                     null: false
    t.datetime "updated_at",                     null: false
  end

end
